// ReSharper disable CppClangTidyClangDiagnosticExtraSemiStmt
// ReSharper disable CppClangTidyMiscUseAnonymousNamespace
#include <ntddk.h>
#include <wdm.h>
#include <wdf.h>
#include <ntstrsafe.h>

#include "DataAcquisition.hpp"
#include "RegistryKey.hpp"
#include "WdfObjectScopeGuard.hpp"

// TODO: https://learn.microsoft.com/en-us/windows-hardware/drivers/wdf/introduction-to-registry-keys-for-drivers
//  set default from inf

// ReSharper disable line StringLiteralTypo
constexpr wchar_t NQN_DEFAULT[] = L"nqn.2025-08.cz.bitzanf.nvmeof:wdf_mock_driver";
constexpr UNICODE_STRING NQN_KEY_NAME = RTL_CONSTANT_STRING(L"NQN");
constexpr UNICODE_STRING NQN_DEFAULT_UNICODE_STRING = RTL_CONSTANT_STRING(NQN_DEFAULT);

constexpr UNICODE_STRING ADDRESS_FAMILY_KEY_NAME = RTL_CONSTANT_STRING(L"AddressFamily");
constexpr UNICODE_STRING TRANSPORT_ADDRESS_KEY_NAME = RTL_CONSTANT_STRING(L"TransportAddress");
constexpr UNICODE_STRING TRANSPORT_SERVICE_ID_KEY_NAME = RTL_CONSTANT_STRING(L"TransportServiceId");
constexpr UNICODE_STRING TRANSPORT_TYPE_KEY_NAME = RTL_CONSTANT_STRING(L"TransportType");

constexpr UNICODE_STRING NT_OBJECT_PATH_KEY_NAME = RTL_CONSTANT_STRING(L"NtObjectPath");

constexpr UNICODE_STRING CONNECTION_STATUS_KEY_NAME = RTL_CONSTANT_STRING(L"_ConnectionStatus");

#define CREATE_SCOPED_WDF_STRING(wdfParentObject, pWdfString, pUnicodeString) { \
    WDF_OBJECT_ATTRIBUTES attributes; \
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes); \
    attributes.ParentObject = wdfParentObject; \
    CHECK_STATUS(WdfStringCreate((pUnicodeString), &attributes, (pWdfString))); \
    }

struct ConnectionKey {
    ConnectionKey() = default;

    ConnectionKey(ConnectionKey&) = delete;
    ConnectionKey& operator=(ConnectionKey&) = delete;

    ConnectionKey(ConnectionKey&& other) noexcept { *this = Utils::Move(other); }
    ConnectionKey& operator=(ConnectionKey&& other) noexcept {
        DriverKey = Utils::Move(other.DriverKey);
        ConnectionsKey = Utils::Move(other.ConnectionsKey);
        SpecificKey = Utils::Move(other.SpecificKey);
        UuidString = Utils::Move(other.UuidString);
        UuidStringRelease = RaiiReleaseHook(&UuidString, RtlFreeUnicodeString);

        other.UuidStringRelease.Deactivate();
        return *this;
    }

    [[nodiscard]] WDFKEY GetKey() const { return SpecificKey.GetKey(); }

    void Close() {
        DriverKey.Close();
        ConnectionsKey.Close();
        SpecificKey.Close();
        UuidStringRelease.Close();
    }

    void DeactivateConnection() { SpecificKey.Deactivate(); }

    static NTSTATUS Initialize(ConnectionKey& connection, WDFDEVICE device) {
        CHECK_STATUS(RegistryKey::Open(device, KEY_READ, connection.DriverKey));
        CHECK_STATUS(connection.DriverKey.OpenSubKey(&CONNECTIONS_KEY_PATH, KEY_WRITE, connection.ConnectionsKey));
        return STATUS_SUCCESS;
    }

    NTSTATUS CreateConnection(const UUID& uuid) {
        UuidStringRelease = RaiiReleaseHook(&UuidString, RtlFreeUnicodeString);

        CHECK_STATUS(RtlStringFromGUID(uuid, &UuidString));

        CHECK_STATUS(ConnectionsKey.CreateSubKey(&UuidString, KEY_READ | KEY_WRITE, SpecificKey));
        return STATUS_SUCCESS;
    }

    NTSTATUS OpenConnection(const UUID& uuid) {
        UuidStringRelease = RaiiReleaseHook(&UuidString, RtlFreeUnicodeString);

        CHECK_STATUS(RtlStringFromGUID(uuid, &UuidString));

        CHECK_STATUS(ConnectionsKey.OpenSubKey(&UuidString, KEY_READ | KEY_WRITE, SpecificKey));
        return STATUS_SUCCESS;
    }

    ~ConnectionKey() { Close(); }

private:
    RegistryKey DriverKey, ConnectionsKey, SpecificKey;
    UNICODE_STRING UuidString = { 0, 0, nullptr };
    RaiiReleaseHook<UNICODE_STRING, decltype(&RtlFreeUnicodeString)> UuidStringRelease = { nullptr, nullptr };
};

template <typename TCallback>
    requires requires (TCallback t, UNICODE_STRING* pStr, const RegistryKey& connectionsKey) {
        { t(connectionsKey, pStr) } -> Concepts::SameAs<NTSTATUS>;
}
static NTSTATUS EnumerateKeys(WDFDEVICE device, TCallback&& callback) {  // NOLINT(cppcoreguidelines-missing-std-forward)
    RegistryKey driverKey, connectionsKey;
    CHECK_STATUS(RegistryKey::Open(device, KEY_READ, driverKey));
    CHECK_STATUS(driverKey.OpenSubKey(&CONNECTIONS_KEY_PATH, KEY_READ, connectionsKey));

    ULONG _;
    KEY_FULL_INFORMATION fullInfo;
    auto keyHandle = WdfRegistryWdmGetHandle(connectionsKey.GetKey());
    CHECK_STATUS(ZwQueryKey(keyHandle, KeyFullInformation, &fullInfo, sizeof(fullInfo), &_));

    auto& tempMemory = FdoGetContext(device)->StringTempMemory;
    auto pKeyInfo = reinterpret_cast<KEY_BASIC_INFORMATION*>(tempMemory.Data);

    for (ULONG i = 0; i < fullInfo.SubKeys; i++) {
        ULONG actualLength;
        CHECK_STATUS(
            ZwEnumerateKey(
                keyHandle,
                i,
                KeyBasicInformation,
                pKeyInfo,
                static_cast<ULONG>(tempMemory.ByteLength()),
                &actualLength
            )
        );

        UNICODE_STRING keyName;
        keyName.Buffer = reinterpret_cast<PWCH>(&pKeyInfo->Name);
        keyName.Length = keyName.MaximumLength = static_cast<USHORT>(pKeyInfo->NameLength);

        CHECK_STATUS(callback(connectionsKey, &keyName));
    }

    return STATUS_SUCCESS;
}

static NTSTATUS AddSingleConnectionSizeInternal(const RegistryKey& connectionsKey, PUNICODE_STRING keyName, size_t& size) {
    size += sizeof(int) + keyName->Length;  // key name
    size += 16; // guid
    size += sizeof(int) + sizeof(int) + sizeof(USHORT); // network connection

    RegistryKey key;
    CHECK_STATUS(connectionsKey.OpenSubKey(keyName, KEY_READ, key));

    ULONG valueLength;
    auto status = WdfRegistryQueryValue(key.GetKey(), &TRANSPORT_ADDRESS_KEY_NAME, 0, nullptr, &valueLength, nullptr);
    // we pass Length as 0, so even successful acquisition will give STATUS_BUFFER_OVERFLOW, but since Value is nullptr, valueLength will still give us the actual length
    if (!NT_SUCCESS(status) && status != STATUS_BUFFER_OVERFLOW) CHECK_STATUS(status);
    size += valueLength + sizeof(int);  // network address string

    status = WdfRegistryQueryValue(key.GetKey(), &NQN_KEY_NAME, 0, nullptr, &valueLength, nullptr);
    if (!NT_SUCCESS(status) && status != STATUS_BUFFER_OVERFLOW) CHECK_STATUS(status);
    size += valueLength + sizeof(int);  // nqn

    status = WdfRegistryQueryValue(key.GetKey(), &NT_OBJECT_PATH_KEY_NAME, 0, nullptr, &valueLength, nullptr);
    if (!NT_SUCCESS(status) && status != STATUS_BUFFER_OVERFLOW) CHECK_STATUS(status);
    size += valueLength + sizeof(int);  // nt object path

    return STATUS_SUCCESS;
}

static NTSTATUS LoadDiskDescriptor(DTO::DiskDescriptor& descriptor, const RegistryKey& connectionsKey, PUNICODE_STRING keyName, const WdfObjectScopeGuard& guard) {
    RegistryKey key;
    CHECK_STATUS(connectionsKey.OpenSubKey(keyName, KEY_READ, key));

    // UUID
    CHECK_STATUS(RtlGUIDFromString(keyName, &descriptor.Uuid));

    // NQN
    UNICODE_STRING unicodeString;
    WDFSTRING string = nullptr;
    CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, nullptr);
    CHECK_STATUS(WdfRegistryQueryString(key.GetKey(), &NQN_KEY_NAME, string));
    WdfStringGetUnicodeString(string, &unicodeString);
    descriptor.Nqn = unicodeString;

    // Transport Address
    string = nullptr;
    CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, nullptr);
    CHECK_STATUS(WdfRegistryQueryString(key.GetKey(), &TRANSPORT_ADDRESS_KEY_NAME, string));
    WdfStringGetUnicodeString(string, &unicodeString);
    descriptor.NetworkConnection.TransportAddress = unicodeString;

    // NT Object Path
    string = nullptr;
    CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, nullptr);
    CHECK_STATUS(WdfRegistryQueryString(key.GetKey(), &NT_OBJECT_PATH_KEY_NAME, string));
    WdfStringGetUnicodeString(string, &unicodeString);
    descriptor.NtObjectPath = unicodeString;

    // Transport Service ID
    ULONG value;
    CHECK_STATUS(WdfRegistryQueryULong(key.GetKey(), &TRANSPORT_SERVICE_ID_KEY_NAME, &value));
    descriptor.NetworkConnection.TransportServiceId = static_cast<UINT16>(value);

    // Transport Type
    CHECK_STATUS(WdfRegistryQueryULong(key.GetKey(), &TRANSPORT_TYPE_KEY_NAME, &value));
    descriptor.NetworkConnection.TransportType = static_cast<DTO::TransportType>(value);

    // Address Family
    CHECK_STATUS(WdfRegistryQueryULong(key.GetKey(), &ADDRESS_FAMILY_KEY_NAME, &value));
    descriptor.NetworkConnection.AddressFamily = static_cast<DTO::AddressFamily>(value);

    return STATUS_SUCCESS;
}

namespace DataAcquisition {
    NTSTATUS GetStatistics(Span<BYTE> outputMemory, size_t& actuallyWritten) {
        if (outputMemory.Length < 16) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Insufficient output buffer (%llu B)\n", outputMemory.Length));
            return STATUS_UNSUCCESSFUL;
        }

        auto p = outputMemory.Data;
        // IEEE754 Single = 3.141593
        // 🥀
        *p++ = 0xDB;
        *p++ = 0x0F;
        *p++ = 0x49;
        *p++ = 0x40;

        UINT32 averageRequestSize = 512;
        UINT64 totalDataTransferred = 50ull * 1024 * 1024 * 1024;

        memcpy(p, &averageRequestSize, sizeof(averageRequestSize));
        p += sizeof(averageRequestSize);

        memcpy(p, &totalDataTransferred, sizeof(totalDataTransferred));

        actuallyWritten = 16;
        return STATUS_SUCCESS;
    }

    NTSTATUS GetHostNqn(WDFDEVICE device, UNICODE_STRING& nqn, const WdfObjectScopeGuard& guard) {
        RegistryKey driverKey, settingsKey;
        CHECK_STATUS(RegistryKey::Open(device, KEY_READ, driverKey));
        CHECK_STATUS(driverKey.OpenSubKey(&SETTINGS_KEY_PATH, KEY_READ | KEY_WRITE, settingsKey));

        WDFSTRING string = nullptr;
        CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, nullptr);

        NTSTATUS status = WdfRegistryQueryString(settingsKey.GetKey(), &NQN_KEY_NAME, string);
        if (status == STATUS_OBJECT_NAME_NOT_FOUND) {
            // We need to create the key and assign a default value
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "NQN Registry key not found, creating default\n"));

            settingsKey.Close();
            driverKey.Close();
            CHECK_STATUS(SetHostNqn(device, NQN_DEFAULT_UNICODE_STRING, guard));

            RtlInitUnicodeString(&nqn, NQN_DEFAULT);

            return STATUS_SUCCESS;
        }

        CHECK_STATUS(status);
        WdfStringGetUnicodeString(string, &nqn);
        return STATUS_SUCCESS;
    }

    NTSTATUS SetHostNqn(WDFDEVICE device, const UNICODE_STRING& nqn, const WdfObjectScopeGuard& guard) {
        RegistryKey driverKey, settingsKey;
        CHECK_STATUS(RegistryKey::Open(device, KEY_READ, driverKey));

        WDFSTRING string = nullptr;
        CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, &nqn);

        CHECK_STATUS(driverKey.OpenSubKey(&SETTINGS_KEY_PATH, KEY_READ | KEY_WRITE, settingsKey));

        return WdfRegistryAssignString(settingsKey.GetKey(), &NQN_KEY_NAME, string);
    }

    NTSTATUS WriteDescriptor(WDFDEVICE device, const UUID& uuid, DTO::DiskDescriptor& descriptor, const bool setInternalState, const WdfObjectScopeGuard& guard) {
        ConnectionKey key;
        CHECK_STATUS(ConnectionKey::Initialize(key, device));
        CHECK_STATUS(key.CreateConnection(uuid));

        WDFSTRING string = nullptr;
        CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, &descriptor.Nqn);
        CHECK_STATUS(WdfRegistryAssignString(key.GetKey(), &NQN_KEY_NAME, string));

        string = nullptr;
        CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, &descriptor.NetworkConnection.TransportAddress);
        CHECK_STATUS(WdfRegistryAssignString(key.GetKey(), &TRANSPORT_ADDRESS_KEY_NAME, string));

        CHECK_STATUS(WdfRegistryAssignULong(key.GetKey(), &TRANSPORT_SERVICE_ID_KEY_NAME, descriptor.NetworkConnection.TransportServiceId));
        CHECK_STATUS(WdfRegistryAssignULong(key.GetKey(), &TRANSPORT_TYPE_KEY_NAME, static_cast<ULONG>(descriptor.NetworkConnection.TransportType)));
        CHECK_STATUS(WdfRegistryAssignULong(key.GetKey(), &ADDRESS_FAMILY_KEY_NAME, static_cast<ULONG>(descriptor.NetworkConnection.AddressFamily)));

        if (setInternalState) {
            // Only for testing purposes, it will actually be stored in memory in the real driver
            CHECK_STATUS(WdfRegistryAssignULong(key.GetKey(), &CONNECTION_STATUS_KEY_NAME, static_cast<ULONG>(DTO::ConnectionStatus::Disconnected)));

            string = nullptr;
            auto fdo = FdoGetContext(device);
            UNICODE_STRING unicodeString = fdo->MakeTempString();
            CHECK_STATUS(RtlUnicodeStringPrintf(&unicodeString, L"\\Device\\Virtualdisk%d", fdo->DiskCount++));
            CREATE_SCOPED_WDF_STRING(guard.GetObject(), &string, &unicodeString);
            CHECK_STATUS(WdfRegistryAssignString(key.GetKey(), &NT_OBJECT_PATH_KEY_NAME, string));
        }

        return STATUS_SUCCESS;
    }

    NTSTATUS RemoveConnection(WDFDEVICE device, const UUID& uuid) {
        ConnectionKey key;
        CHECK_STATUS(ConnectionKey::Initialize(key, device));

        auto status = key.OpenConnection(uuid);
        if (status != STATUS_OBJECT_NAME_NOT_FOUND) {
            CHECK_STATUS(status);

            // Remove all values first
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &NQN_KEY_NAME));
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &TRANSPORT_ADDRESS_KEY_NAME));
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &TRANSPORT_SERVICE_ID_KEY_NAME));
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &TRANSPORT_TYPE_KEY_NAME));
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &ADDRESS_FAMILY_KEY_NAME));
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &CONNECTION_STATUS_KEY_NAME));
            CHECK_STATUS(WdfRegistryRemoveValue(key.GetKey(), &NT_OBJECT_PATH_KEY_NAME));

            CHECK_STATUS(WdfRegistryRemoveKey(key.GetKey()));
            key.DeactivateConnection();
        }

        return STATUS_SUCCESS;
    }

    NTSTATUS GetAllConnectionsSize(WDFDEVICE device, size_t& size) {
        size = sizeof(int); // array count
        return EnumerateKeys(device, [&size](const RegistryKey& connectionsKey, PUNICODE_STRING keyName) {
            return AddSingleConnectionSizeInternal(connectionsKey, keyName, size);
            });
    }

    NTSTATUS GetConnectionSize(WDFDEVICE device, const UUID& uuid, size_t& size) {
        UNICODE_STRING uuidString;
        CHECK_STATUS(RtlStringFromGUID(uuid, &uuidString));
        RaiiReleaseHook uuidStringRelease(&uuidString, RtlFreeUnicodeString);

        RegistryKey driverKey, connectionsKey;
        CHECK_STATUS(RegistryKey::Open(device, KEY_READ, driverKey));
        CHECK_STATUS(driverKey.OpenSubKey(&CONNECTIONS_KEY_PATH, KEY_READ, connectionsKey));

        size = 0;
        return AddSingleConnectionSizeInternal(connectionsKey, &uuidString, size);
    }

    NTSTATUS GetConnection(WDFDEVICE device, Span<BYTE> outputMemory, size_t& actuallyWritten, const UUID& uuid, const WdfObjectScopeGuard& guard) {
        RegistryKey driverKey, connectionsKey;
        CHECK_STATUS(RegistryKey::Open(device, KEY_READ, driverKey));
        CHECK_STATUS(driverKey.OpenSubKey(&CONNECTIONS_KEY_PATH, KEY_READ, connectionsKey));

        UNICODE_STRING uuidString;
        CHECK_STATUS(RtlStringFromGUID(uuid, &uuidString));
        RaiiReleaseHook uuidStringRelease(&uuidString, RtlFreeUnicodeString);

        DTO::DiskDescriptor descriptor;
        CHECK_STATUS(LoadDiskDescriptor(descriptor, connectionsKey, &uuidString, guard));

        if (outputMemory.Length < descriptor.GetRequiredBufferSizeFull()) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Insufficient output buffer (%llu B)\n", outputMemory.Length));
            return STATUS_UNSUCCESSFUL;
        }

        auto written = descriptor.WriteFull(outputMemory, 0);
        if (written == 0) return STATUS_UNSUCCESSFUL;
        actuallyWritten = written;
        return STATUS_SUCCESS;
    }

    NTSTATUS GetConnectionStatus(WDFDEVICE device, DTO::ConnectionStatus& status, const UUID& uuid) {
        ConnectionKey key;
        CHECK_STATUS(ConnectionKey::Initialize(key, device));
        CHECK_STATUS(key.OpenConnection(uuid));

        ULONG value;
        CHECK_STATUS(WdfRegistryQueryULong(key.GetKey(), &CONNECTION_STATUS_KEY_NAME, &value));
        status = static_cast<DTO::ConnectionStatus>(value);

        return STATUS_SUCCESS;
    }

    NTSTATUS GetAllConnections(WDFDEVICE device, Span<BYTE> outputMemory, size_t& actuallyWritten, const WdfObjectScopeGuard& guard) {
        if (outputMemory.Length < sizeof(int)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Insufficient output buffer (%llu B)\n", outputMemory.Length));
            return STATUS_UNSUCCESSFUL;
        }
        actuallyWritten = sizeof(int);
        int iSize = 0;
        memcpy(outputMemory.Data, &iSize, sizeof(int));

        CHECK_STATUS(
            EnumerateKeys(
                device,
                [&](const RegistryKey& connectionsKey, PUNICODE_STRING keyName) {
                    DTO::DiskDescriptor descriptor;
                    CHECK_STATUS(LoadDiskDescriptor(descriptor, connectionsKey, keyName, guard));

                    auto written = descriptor.WriteFull(outputMemory, actuallyWritten);
                    if (written == 0) return STATUS_UNSUCCESSFUL;

                    actuallyWritten += written;
                    iSize++;
                    return STATUS_SUCCESS;
                }
            )
        );

        memcpy(outputMemory.Data, &iSize, sizeof(int));
        return STATUS_SUCCESS;
    }
}
