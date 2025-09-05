#include "RegistryKey.hpp"
#include "Types.hpp"

#include <wdf.h>
#include <wdm.h>

NTSTATUS RegistryKey::Open(WDFDEVICE device, ACCESS_MASK access, RegistryKey& key)
{
    RegistryKey temp;
    auto status = WdfDeviceOpenRegistryKey(
        device,
        PLUGPLAY_REGKEY_DRIVER,
        access,
        WDF_NO_OBJECT_ATTRIBUTES,
        &temp.Key
    );

    if (NT_SUCCESS(status)) key = Utils::Move(temp);
    return status;
}

RegistryKey::RegistryKey(RegistryKey&& other) noexcept {
    *this = static_cast<RegistryKey&&>(other);
}

RegistryKey& RegistryKey::operator=(RegistryKey&& other) noexcept {
    // Close potentially open key
    Close();

    // Steal other's key
    Key = other.Key;
    other.Key = nullptr;
    return *this;
}

RegistryKey::~RegistryKey() {
    Close();
}

void RegistryKey::Close() {
    if (Key) {
        WdfRegistryClose(Key);
        Key = nullptr;
    }
}

void RegistryKey::Deactivate() {
    Key = nullptr;
}

NTSTATUS RegistryKey::OpenSubKey(const UNICODE_STRING* keyName, ACCESS_MASK access, RegistryKey& key) const {
    RegistryKey temp;
    auto status = WdfRegistryOpenKey(Key, keyName, access, WDF_NO_OBJECT_ATTRIBUTES, &temp.Key);

    if (NT_SUCCESS(status)) key = Utils::Move(temp);
    return status;
}

NTSTATUS RegistryKey::CreateSubKey(const UNICODE_STRING* keyName, ACCESS_MASK access, RegistryKey& key) const {
    RegistryKey temp;
    auto status = WdfRegistryCreateKey(Key, keyName, access, REG_OPTION_NON_VOLATILE, nullptr, WDF_NO_OBJECT_ATTRIBUTES, &temp.Key);

    if (NT_SUCCESS(status)) key = Utils::Move(temp);
    return status;
}
