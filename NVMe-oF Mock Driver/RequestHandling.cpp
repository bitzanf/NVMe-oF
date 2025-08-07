// ReSharper disable CppClangTidyMiscUseAnonymousNamespace
#include "RequestHandling.hpp"
#include "DataAcquisition.hpp"

#include <ntstatus.h>
#include <wdm.h>
#include <wdf.h>

#define REQUEST_HANDLER_HEADER(name) \
static NTSTATUS name (WDFDEVICE device, WDFREQUEST request, Span<BYTE> requestMemory, size_t outputBufferSize, size_t& written) { \
    UNREFERENCED_PARAMETER(device); UNREFERENCED_PARAMETER(request); UNREFERENCED_PARAMETER(requestMemory); UNREFERENCED_PARAMETER(outputBufferSize); UNREFERENCED_PARAMETER(written); \
    KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR #name "\n"));

#define CHECK_STATUS(expr) { NTSTATUS _temp_status = (expr); if (!NT_SUCCESS(_temp_status)) return _temp_status; }

#define HANDLE_REQUEST(rq) status = rq (device, request, requestMemory, outputBufferSize, written); break

static NTSTATUS GetOutputBuffer(WDFREQUEST request, size_t sizeExpected, NvmeOFMockDriver::Span<BYTE>& output) {
    WDFMEMORY memory;
    CHECK_STATUS(WdfRequestRetrieveOutputMemory(request, &memory))

    size_t size;
    auto bfr = WdfMemoryGetBuffer(memory, &size);
    if (sizeExpected != size) {
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "memory:size (%llu) != wdf:size (%llu)\n", size, sizeExpected));
        return STATUS_UNSUCCESSFUL;
    }

    output = {
        .Data = static_cast<BYTE*>(bfr),
        .Length = size
    };

    return STATUS_SUCCESS;
}

namespace NvmeOFMockDriver {
    namespace RequestHandlers {

        REQUEST_HANDLER_HEADER(GetHostNqn)
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetStatistics)
            Span<BYTE> buffer;

            CHECK_STATUS(GetOutputBuffer(request, outputBufferSize, buffer))
            CHECK_STATUS(DataAcquisition::GetStatistics(buffer, written))

            return STATUS_SUCCESS;
        }
    }

    NTSTATUS HandleApplicationRequest(WDFDEVICE device, WDFREQUEST request, DriverRequestType applicationRequest, Span<BYTE> requestMemory, size_t outputBufferSize, size_t& written) {
        using namespace RequestHandlers;
        written = 0;

        NTSTATUS status = STATUS_UNSUCCESSFUL;
    
        switch (applicationRequest) {
            case DriverRequestType::None: break;

            case DriverRequestType::GetHostNqn: HANDLE_REQUEST(GetHostNqn);
            case DriverRequestType::SetHostNqn: break;
            case DriverRequestType::GetAllConnections: break;
            case DriverRequestType::AddConnection: break;
            case DriverRequestType::RemoveConnection: break;
            case DriverRequestType::ModifyConnection: break;
            case DriverRequestType::GetConnectionStatus: break;
            case DriverRequestType::GetConnection: break;
            case DriverRequestType::DiscoveryRequest: break;
            case DriverRequestType::GetDiscoveryResponse: break;
            case DriverRequestType::GetStatistics: HANDLE_REQUEST(GetStatistics);
            case DriverRequestType::GetHostNqnSize: break;
            case DriverRequestType::GetConnectionSize: break;
            case DriverRequestType::GetAllConnectionsSize: break;
            case DriverRequestType::GetDiscoveryResponseSize: break;
        }

        if (!NT_SUCCESS(status)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR __FUNCTION__ " ERROR Status: 0x%x\n", status));
        }

        return status;
    }
}
