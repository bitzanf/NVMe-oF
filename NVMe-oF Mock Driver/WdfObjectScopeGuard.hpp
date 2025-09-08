#pragma once
#include <ntdef.h>

#include "DTOs.hpp"
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

template <typename T, typename CallableRelease>
requires requires (T* inst, CallableRelease release) {
    { release(inst) } -> Concepts::SameAs<void>;
}
class RaiiReleaseHook {
public:
    RaiiReleaseHook(T* instance, CallableRelease release) : Instance(instance), Release(Utils::Move(release)) {}

    RaiiReleaseHook(RaiiReleaseHook&) = delete;
    RaiiReleaseHook& operator=(RaiiReleaseHook&) = delete;

    RaiiReleaseHook(RaiiReleaseHook&& other) noexcept { *this = Utils::Move(other); }
    RaiiReleaseHook& operator=(RaiiReleaseHook&& other) noexcept{
        Instance = other.Instance;
        Release = Utils::Move(other.Release);

        other.Instance = nullptr;
        return *this;
    }

    auto Get() { return Instance; }

    void Deactivate() { Instance = nullptr; }

    void Close() {
        if (Instance) {
            if (Release) Release(Instance);
            else KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Close() called without any release callback!\n"));

            Instance = nullptr;
        }
    }

    ~RaiiReleaseHook() { Close(); }

private:
    T* Instance;
    CallableRelease Release;
};