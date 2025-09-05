#pragma once
#include <ntddk.h>

#include "Types.hpp"
#include "WdfObjectScopeGuard.hpp"
#include "DTOs.hpp"

namespace NvmeOFMockDriver::DataAcquisition {
    NTSTATUS GetStatistics(Span<BYTE> outputMemory, size_t& actuallyWritten);
    NTSTATUS GetHostNqn(WDFDEVICE device, UNICODE_STRING& nqn, const WdfObjectScopeGuard& guard);
    NTSTATUS SetHostNqn(WDFDEVICE device, const UNICODE_STRING& nqn, const WdfObjectScopeGuard& guard);
    NTSTATUS WriteDescriptor(WDFDEVICE device, const UUID& uuid, DTO::DiskDescriptor& descriptor, const WdfObjectScopeGuard& guard);
    NTSTATUS RemoveConnection(WDFDEVICE device, const UUID& uuid);
}
