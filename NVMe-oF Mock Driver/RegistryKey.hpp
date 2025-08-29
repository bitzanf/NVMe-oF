#pragma once
#include <ntdef.h>
#include <wdftypes.h>
#include <wdm.h>

class RegistryKey {
public:
    static NTSTATUS Open(WDFDEVICE device, ACCESS_MASK access, RegistryKey& key);

    RegistryKey() = default;

    RegistryKey(RegistryKey&) = delete;
    RegistryKey& operator=(RegistryKey&) = delete;

    RegistryKey(RegistryKey&& other) noexcept;
    RegistryKey& operator=(RegistryKey&& other) noexcept;

    ~RegistryKey();
    void Close();

    [[nodiscard]] WDFKEY GetKey() const { return Key; }

    /// Open an existing key
    NTSTATUS OpenSubKey(const UNICODE_STRING *keyName, ACCESS_MASK access, RegistryKey& key) const;

    /// Open an existing key, or create if it does not exist yet
    NTSTATUS CreateSubKey(const UNICODE_STRING* keyName, ACCESS_MASK access, RegistryKey& key) const;

private:
    WDFKEY Key = nullptr;
};
