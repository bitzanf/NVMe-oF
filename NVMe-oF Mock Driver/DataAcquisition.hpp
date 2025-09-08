#pragma once
#include <ntddk.h>

#include "Types.hpp"
#include "WdfObjectScopeGuard.hpp"
#include "DTOs.hpp"

namespace DataAcquisition {
    NTSTATUS GetStatistics(Span<BYTE> outputMemory, size_t& actuallyWritten);
    NTSTATUS GetHostNqn(WDFDEVICE device, UNICODE_STRING& nqn, const WdfObjectScopeGuard& guard);
    NTSTATUS SetHostNqn(WDFDEVICE device, const UNICODE_STRING& nqn, const WdfObjectScopeGuard& guard);
    NTSTATUS WriteDescriptor(WDFDEVICE device, const UUID& uuid, DTO::DiskDescriptor& descriptor, bool setInternalState, const WdfObjectScopeGuard& guard);
    NTSTATUS RemoveConnection(WDFDEVICE device, const UUID& uuid);
    NTSTATUS GetAllConnectionsSize(WDFDEVICE device, size_t& size);
    NTSTATUS GetConnectionSize(WDFDEVICE device, const UUID& uuid, size_t& size);
    NTSTATUS GetConnection(WDFDEVICE device, Span<BYTE> outputMemory, size_t& actuallyWritten, const UUID& uuid, const WdfObjectScopeGuard& guard);
    NTSTATUS GetConnectionStatus(WDFDEVICE device, DTO::ConnectionStatus& status, const UUID& uuid);
    NTSTATUS GetAllConnections(WDFDEVICE device, Span<BYTE> outputMemory, size_t& actuallyWritten, const WdfObjectScopeGuard& guard);
}
