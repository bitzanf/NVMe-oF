// ReSharper disable CppClangTidyMiscUseAnonymousNamespace
// ReSharper disable CppClangTidyClangDiagnosticExtraSemiStmt
#include "RequestHandling.hpp"
#include "DataAcquisition.hpp"

#include <ntstatus.h>
#include <wdm.h>
#include <wdf.h>

#define REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS \
    UNREFERENCED_PARAMETER(device); UNREFERENCED_PARAMETER(request); UNREFERENCED_PARAMETER(requestMemory); UNREFERENCED_PARAMETER(outputBufferSize); UNREFERENCED_PARAMETER(written);

#define REQUEST_HANDLER_HEADER(name) \
    static NTSTATUS name (WDFDEVICE device, WDFREQUEST request, Span<BYTE> requestMemory, size_t outputBufferSize, size_t& written)

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

static const char* RequestToString(DriverRequestType request) {
    switch (request) {
    case DriverRequestType::None: return "None";

    case DriverRequestType::GetHostNqn: return "GetHostNqn";
    case DriverRequestType::SetHostNqn: return "SetHostNqn";
    case DriverRequestType::GetAllConnections: return "GetAllConnections";
    case DriverRequestType::AddConnection: return "AddConnection";
    case DriverRequestType::RemoveConnection: return "RemoveConnection";
    case DriverRequestType::ModifyConnection: return "ModifyConnection";
    case DriverRequestType::GetConnectionStatus: return "GetConnectionStatus";
    case DriverRequestType::GetConnection: return "GetConnection";
    case DriverRequestType::DiscoveryRequest: return "DiscoveryRequest";
    case DriverRequestType::GetDiscoveryResponse: return "GetDiscoveryResponse";
    case DriverRequestType::GetStatistics: return "GetStatistics";
    case DriverRequestType::GetHostNqnSize: return "GetHostNqnSize";
    case DriverRequestType::GetConnectionSize: return "GetConnectionSize";
    case DriverRequestType::GetAllConnectionsSize: return "GetAllConnectionsSize";
    case DriverRequestType::GetDiscoveryResponseSize: return "GetDiscoveryResponseSize";
    }

    return "<unknown>";
}

static size_t WriteString(NvmeOFMockDriver::Span<BYTE> buffer, const UNICODE_STRING& string, const size_t offset = 0) {
    const UINT32 len = string.Length;
    auto p = buffer.Data + offset;

    memcpy(p, &len, sizeof(len));
    p += sizeof(len);

    memcpy(p, string.Buffer, len);
    return sizeof(len) + len;
}

static size_t ReadString(UNICODE_STRING& string, const NvmeOFMockDriver::Span<BYTE> buffer) {
    UINT32 len;
    if (buffer.Length < sizeof(len)) {
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small)\n"));
        return 0;
    }

    memcpy(&len, buffer.Data, sizeof(len));
    if (len >= 0xffff) {
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (string too long)\n"));
        return 0;
    }

    if (len > buffer.Length - sizeof(len)) {
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " Invalid buffer (too small for given string length)\n"));
        return 0;
    }

    if (len > string.MaximumLength) {
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR __FUNCTION__ " String is of insufficient size for the given data\n"));
        return 0;
    }

    const auto strBytes = min(len, string.MaximumLength);

    memcpy(string.Buffer, buffer.Data + sizeof(len), strBytes);
    string.Length = static_cast<USHORT>(strBytes);

    return strBytes + sizeof(len);
}

namespace NvmeOFMockDriver {
    namespace RequestHandlers {

        REQUEST_HANDLER_HEADER(GetHostNqn) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;
            Span<BYTE> buffer;

            CHECK_STATUS(GetOutputBuffer(request, outputBufferSize, buffer));

            WdfObjectScopeGuard guard;
            CHECK_STATUS(WdfObjectScopeGuard::Create(guard, device));

            UNICODE_STRING nqn;
            CHECK_STATUS(DataAcquisition::GetHostNqn(device, nqn, guard));

            auto nqnBytes = nqn.Length + sizeof(UINT32);
            if (buffer.Length < nqnBytes) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Request buffer size too small (need at least %llu bytes for NQN, got %llu)!\n", nqnBytes, buffer.Length));
                return STATUS_UNSUCCESSFUL;
            }

            written = WriteString(buffer, nqn);
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetStatistics) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            Span<BYTE> buffer;

            CHECK_STATUS(GetOutputBuffer(request, outputBufferSize, buffer))
            CHECK_STATUS(DataAcquisition::GetStatistics(buffer, written))

            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(SetHostNqn) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;
            WdfObjectScopeGuard guard;
            CHECK_STATUS(WdfObjectScopeGuard::Create(guard, device));

            auto fdo = FdoGetContext(device);

            UNICODE_STRING nqn = fdo->MakeTempString();
            ReadString(nqn, requestMemory);

            CHECK_STATUS(DataAcquisition::SetHostNqn(device, nqn, guard));

            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetAllConnections) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(AddConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(RemoveConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(ModifyConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetConnectionStatus) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(DiscoveryRequest) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetDiscoveryResponse) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetConnectionSize) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetHostNqnSize) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            Span<BYTE> buffer;

            CHECK_STATUS(GetOutputBuffer(request, outputBufferSize, buffer))

            if (buffer.Length < sizeof(int)) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Request buffer size too small!\n"));
                return STATUS_UNSUCCESSFUL;
            }

            WdfObjectScopeGuard guard;
            CHECK_STATUS(WdfObjectScopeGuard::Create(guard, device));

            UNICODE_STRING nqn;
            CHECK_STATUS(DataAcquisition::GetHostNqn(device, nqn, guard));

            int size = static_cast<int>(nqn.Length + sizeof(UINT32));
            memcpy(buffer.Data, &size, sizeof(int));
            written = sizeof(int);

            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetAllConnectionsSize) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetDiscoveryResponseSize) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_SUCCESS;
        }
    }

    NTSTATUS HandleApplicationRequest(WDFDEVICE device, WDFREQUEST request, DriverRequestType applicationRequest, Span<BYTE> requestMemory, size_t outputBufferSize, size_t& written) {
        using namespace RequestHandlers;
        written = 0;

        NTSTATUS status = STATUS_UNSUCCESSFUL;

        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "Application Request %s (%d)\n", RequestToString(applicationRequest), applicationRequest));
    
        switch (applicationRequest) {
            case DriverRequestType::None: break;

            case DriverRequestType::GetHostNqn: HANDLE_REQUEST(GetHostNqn);
            case DriverRequestType::SetHostNqn: HANDLE_REQUEST(SetHostNqn);
            case DriverRequestType::GetAllConnections: HANDLE_REQUEST(GetAllConnections);
            case DriverRequestType::AddConnection: HANDLE_REQUEST(AddConnection);
            case DriverRequestType::RemoveConnection: HANDLE_REQUEST(RemoveConnection);
            case DriverRequestType::ModifyConnection: HANDLE_REQUEST(ModifyConnection);
            case DriverRequestType::GetConnectionStatus: HANDLE_REQUEST(GetConnectionStatus);
            case DriverRequestType::GetConnection: HANDLE_REQUEST(GetConnection);
            case DriverRequestType::DiscoveryRequest: HANDLE_REQUEST(DiscoveryRequest);
            case DriverRequestType::GetDiscoveryResponse: HANDLE_REQUEST(GetDiscoveryResponse);
            case DriverRequestType::GetStatistics: HANDLE_REQUEST(GetStatistics);
            case DriverRequestType::GetHostNqnSize: HANDLE_REQUEST(GetHostNqnSize);
            case DriverRequestType::GetConnectionSize: HANDLE_REQUEST(GetConnectionSize);
            case DriverRequestType::GetAllConnectionsSize: HANDLE_REQUEST(GetAllConnectionsSize);
            case DriverRequestType::GetDiscoveryResponseSize: HANDLE_REQUEST(GetDiscoveryResponseSize);
        }

        CHECK_STATUS(status)
        return status;
    }
}
