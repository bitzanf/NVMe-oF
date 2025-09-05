// ReSharper disable CppClangTidyClangDiagnosticExtraSemiStmt
#include <ntddk.h>
#include <wdm.h>
#include <wdf.h>

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
        CHECK_STATUS(RegistryKey::Open(device, KEY_READ,  connection.DriverKey));
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
    UNICODE_STRING UuidString = {0, 0, nullptr};
    RaiiReleaseHook<UNICODE_STRING, decltype(&RtlFreeUnicodeString)> UuidStringRelease = {nullptr, nullptr};
};

namespace NvmeOFMockDriver::DataAcquisition {
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

    NTSTATUS WriteDescriptor(WDFDEVICE device, const UUID& uuid, DTO::DiskDescriptor& descriptor, const WdfObjectScopeGuard& guard) {
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

        // Only for testing purposes, it will actually be stored in memory in the real driver
        CHECK_STATUS(WdfRegistryAssignULong(key.GetKey(), &CONNECTION_STATUS_KEY_NAME, static_cast<ULONG>(DTO::ConnectionStatus::Disconnected)));

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

            CHECK_STATUS(WdfRegistryRemoveKey(key.GetKey()));
            key.DeactivateConnection();
        }

        return STATUS_SUCCESS;
    }
}
