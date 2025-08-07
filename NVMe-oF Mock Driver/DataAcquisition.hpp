#pragma once
#include <ntdef.h>
#include <wdftypes.h>

#include "Types.hpp"

namespace NvmeOFMockDriver::DataAcquisition {
    NTSTATUS GetStatistics(Span<BYTE> outputMemory, size_t& actuallyWritten);
}
