#include "WdfObjectScopeGuard.hpp"

NTSTATUS WdfObjectScopeGuard::Create(WdfObjectScopeGuard& out, WDFOBJECT parent) {
    WdfObjectScopeGuard guard;
    NTSTATUS status;

    if (parent) {
        WDF_OBJECT_ATTRIBUTES attributes;
        WDF_OBJECT_ATTRIBUTES_INIT(&attributes);
        attributes.ParentObject = parent;
        status = WdfObjectCreate(&attributes, &guard.object);
    } else {
        status = WdfObjectCreate(WDF_NO_OBJECT_ATTRIBUTES, &guard.object);
    }

    out = Utils::Move(guard);
    return status;
}

WdfObjectScopeGuard::WdfObjectScopeGuard(WdfObjectScopeGuard&& other) noexcept {
    *this = Utils::Move(other);
}

WdfObjectScopeGuard& WdfObjectScopeGuard::operator=(WdfObjectScopeGuard&& other) noexcept {
    Release();

    object = other.object;
    other.object = nullptr;
    return *this;
}

WdfObjectScopeGuard::~WdfObjectScopeGuard() {
    Release();
}

void WdfObjectScopeGuard::Release() {
    if (object) {
        WdfObjectDelete(object);
        object = nullptr;
    }
}
