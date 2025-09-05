// ReSharper disable CppClangTidyMiscUseAnonymousNamespace
// ReSharper disable CppClangTidyClangDiagnosticExtraSemiStmt
#include <ntddk.h>
#include <wdm.h>
#include <wdf.h>

#include "RequestHandling.hpp"
#include "DataAcquisition.hpp"
#include "DTOs.hpp"

#define REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS \
    UNREFERENCED_PARAMETER(device); UNREFERENCED_PARAMETER(request); UNREFERENCED_PARAMETER(requestMemory); UNREFERENCED_PARAMETER(outputBufferSize); UNREFERENCED_PARAMETER(written);

#define REQUEST_HANDLER_HEADER(name) \
    static NTSTATUS name (WDFDEVICE device, WDFREQUEST request, Span<BYTE> requestMemory, size_t outputBufferSize, size_t& written)

#define HANDLE_REQUEST(rq) status = rq (device, request, requestMemory, outputBufferSize, written); break

constexpr NvmeOFMockDriver::DTO::DiskDescriptor DISCOVERY_RESPONSE[]{
    {
        .NetworkConnection = {
            .TransportType = NvmeOFMockDriver::DTO::TransportType::Tcp,
            .AddressFamily = NvmeOFMockDriver::DTO::AddressFamily::IPv4,
            .TransportServiceId = 4420,
            .TransportAddress = RTL_CONSTANT_STRING(L"10.1.0.50")
        },
        .Nqn = RTL_CONSTANT_STRING(L"nqn.2014-08.org.nvmexpress.discovery")
    },
    {
        .NetworkConnection = {
            .TransportType = NvmeOFMockDriver::DTO::TransportType::Tcp,
            .AddressFamily = NvmeOFMockDriver::DTO::AddressFamily::IPv4,
            .TransportServiceId = 4420,
            .TransportAddress = RTL_CONSTANT_STRING(L"10.1.0.50")
        },
        .Nqn = RTL_CONSTANT_STRING(L"zfs-netdisk")
    }
};

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

namespace NvmeOFMockDriver {
    namespace RequestHandlers {

        using namespace DTO;

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
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;
            return STATUS_NOT_IMPLEMENTED;
        }

        REQUEST_HANDLER_HEADER(AddConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;

            UUID uuid;
            CHECK_STATUS(ExUuidCreate(&uuid));

            DiskDescriptor descriptor;
            auto size = DiskDescriptor::Read(descriptor, requestMemory, 0);
            if (size == 0) return STATUS_UNSUCCESSFUL;

            WdfObjectScopeGuard guard;
            CHECK_STATUS(WdfObjectScopeGuard::Create(guard, device));
            CHECK_STATUS(DataAcquisition::WriteDescriptor(device, uuid, descriptor, guard));

            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(RemoveConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;
            UUID uuid;
            if (requestMemory.Length < sizeof(UUID)) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Request buffer size too small!\n"));
                return STATUS_UNSUCCESSFUL;
            }

            memcpy(&uuid, requestMemory.Data, sizeof(UUID));

            return DataAcquisition::RemoveConnection(device, uuid);
        }

        REQUEST_HANDLER_HEADER(ModifyConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;

            UUID uuid;
            if (requestMemory.Length < sizeof(UUID)) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Request buffer size too small!\n"));
                return STATUS_UNSUCCESSFUL;
            }

            memcpy(&uuid, requestMemory.Data, sizeof(UUID));

            DiskDescriptor descriptor;
            auto size = DiskDescriptor::Read(descriptor, requestMemory, sizeof(UUID));
            if (size == 0) return STATUS_UNSUCCESSFUL;

            WdfObjectScopeGuard guard;
            CHECK_STATUS(WdfObjectScopeGuard::Create(guard, device));
            CHECK_STATUS(DataAcquisition::WriteDescriptor(device, uuid, descriptor, guard));

            return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetConnectionStatus) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_NOT_IMPLEMENTED;
        }

        REQUEST_HANDLER_HEADER(GetConnection) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_NOT_IMPLEMENTED;
        }

        REQUEST_HANDLER_HEADER(DiscoveryRequest) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;

            // We don't actually do anything here, since the request response is static...
            // Sleep for 2 seconds to give an illusion of doing something
            LARGE_INTEGER delay;
            delay.QuadPart = 2ll * 10 * 1'000'000;  // 100ns multiples
            return KeDelayExecutionThread(KernelMode, FALSE, &delay);
        }

        REQUEST_HANDLER_HEADER(GetDiscoveryResponse) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;
            Span<BYTE> buffer;

            CHECK_STATUS(GetOutputBuffer(request, outputBufferSize, buffer));
            written = WriteArray(buffer, DISCOVERY_RESPONSE, Utils::GetArrayLength(DISCOVERY_RESPONSE));
            
            if (written == 0) return STATUS_UNSUCCESSFUL;
            else return STATUS_SUCCESS;
        }

        REQUEST_HANDLER_HEADER(GetConnectionSize) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS
            return STATUS_NOT_IMPLEMENTED;
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
            return STATUS_NOT_IMPLEMENTED;
        }

        REQUEST_HANDLER_HEADER(GetDiscoveryResponseSize) {
            REQUEST_HANDLER_HEADER_UNREFERENCED_PARAMS;
            Span<BYTE> buffer;

            CHECK_STATUS(GetOutputBuffer(request, outputBufferSize, buffer));
            if (buffer.Length < sizeof(int)) {
                KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Request buffer size too small!\n"));
                return STATUS_UNSUCCESSFUL;
            }

            int size = sizeof(int);
            for (const auto& descriptor : DISCOVERY_RESPONSE) size += static_cast<int>(descriptor.GetRequiredBufferSize());

            memcpy(buffer.Data, &size, sizeof(int));

            written = sizeof(int);
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
