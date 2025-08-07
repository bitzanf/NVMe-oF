#include "DataAcquisition.hpp"

#include <ntstatus.h>
#include <wdm.h>

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
}