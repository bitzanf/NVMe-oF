// ReSharper disable CppClangTidyClangDiagnosticExtraSemiStmt
#include <ntstatus.h>
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

#define CREATE_SCOPED_WDF_STRING(wdfParentObject, pWdfString, pUnicodeString) { \
    WDF_OBJECT_ATTRIBUTES attributes; \
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes); \
    attributes.ParentObject = wdfParentObject; \
    CHECK_STATUS(WdfStringCreate((pUnicodeString), &attributes, (pWdfString))); \
    }


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
}
