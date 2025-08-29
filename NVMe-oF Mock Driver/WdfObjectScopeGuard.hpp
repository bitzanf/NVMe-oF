#pragma once
#include <ntdef.h>
#include "Types.hpp"

class WdfObjectScopeGuard {
public:
    static NTSTATUS Create(WdfObjectScopeGuard& out, WDFOBJECT parent = nullptr);

    WdfObjectScopeGuard() = default;

    WdfObjectScopeGuard(WdfObjectScopeGuard&) = delete;
    WdfObjectScopeGuard& operator=(WdfObjectScopeGuard&) = delete;

    WdfObjectScopeGuard(WdfObjectScopeGuard&& other) noexcept;
    WdfObjectScopeGuard& operator=(WdfObjectScopeGuard&& other) noexcept;

    ~WdfObjectScopeGuard();

    [[ nodiscard ]] WDFOBJECT GetObject() const { return object; }
private:
    WDFOBJECT object = nullptr;

    void Release();
};
