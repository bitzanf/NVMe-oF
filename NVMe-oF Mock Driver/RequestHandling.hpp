#pragma once
#include <ntdef.h>
#include <wdftypes.h>

#include "Types.hpp"
#include "DriverRequestType.hpp"

namespace NvmeOFMockDriver {
    NTSTATUS HandleApplicationRequest(WDFDEVICE device, WDFREQUEST request, DriverRequestType applicationRequest, Span<BYTE> requestMemory, size_t outputBufferSize, size_t& written);
}
