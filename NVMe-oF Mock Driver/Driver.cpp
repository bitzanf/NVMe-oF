#include <ntddk.h>
#include <wdf.h>
#include <ntddscsi.h>

#include "RequestHandling.hpp"

constexpr UNICODE_STRING DeviceSymlink = RTL_CONSTANT_STRING(L"\\??\\NvmeOfController");

extern "C" DRIVER_INITIALIZE DriverEntry;

namespace NvmeOFMockDriver {
    EVT_WDF_DRIVER_DEVICE_ADD EvtDeviceAdd;
    EVT_WDF_OBJECT_CONTEXT_CLEANUP EvtDriverCleanup;
    EVT_WDF_IO_QUEUE_IO_DEVICE_CONTROL EvtIoQueueIoCtl;

    struct FdoContext {
        WDFQUEUE Queue;
    };

    WDF_DECLARE_CONTEXT_TYPE_WITH_NAME(FdoContext, FdoGetContext);
}

bool CheckStatus(NTSTATUS status) {
    if (!NT_SUCCESS(status)) {
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Status: 0x%x", status));
        return false;
    }
    return true;
}

NTSTATUS DriverEntry(PDRIVER_OBJECT driverObject, PUNICODE_STRING registryPath) {
    KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "DriverEntry\n"));

    WDF_DRIVER_CONFIG config;
    WDF_DRIVER_CONFIG_INIT(&config, NvmeOFMockDriver::EvtDeviceAdd);

    WDF_OBJECT_ATTRIBUTES attributes;
    WDF_OBJECT_ATTRIBUTES_INIT(&attributes);

    attributes.EvtCleanupCallback = &NvmeOFMockDriver::EvtDriverCleanup;

    NTSTATUS status = WdfDriverCreate(
        driverObject,
        registryPath,
        &attributes,
        &config,
        WDF_NO_HANDLE
    );

    CheckStatus(status);
    return status;
}

namespace NvmeOFMockDriver {
    NTSTATUS EvtDeviceAdd(WDFDRIVER driver, PWDFDEVICE_INIT deviceInit) {
        PAGED_CODE();

        UNREFERENCED_PARAMETER(driver);
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL,  DRIVER_LOG_STR "EvtDeviceAdd\n"));

        // Create device object
        WDF_OBJECT_ATTRIBUTES attributes;
        WDF_OBJECT_ATTRIBUTES_INIT_CONTEXT_TYPE(&attributes, FdoContext);

        WDFDEVICE device;
        NTSTATUS status = WdfDeviceCreate(&deviceInit, &attributes, &device);

        auto fdoContext = FdoGetContext(device);

        if (!CheckStatus(status)) goto exit;
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "Created WDF device\n"));

        // Create default IO queue
        WDF_IO_QUEUE_CONFIG queueConfig;
        WDF_IO_QUEUE_CONFIG_INIT(&queueConfig, WdfIoQueueDispatchSequential);
        queueConfig.EvtIoDeviceControl = &EvtIoQueueIoCtl;

        status = WdfIoQueueCreate(device, &queueConfig, WDF_NO_OBJECT_ATTRIBUTES, &(fdoContext->Queue));
        if (!CheckStatus(status)) goto exit;
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "Created WDF IO queue\n"));

        status = WdfDeviceConfigureRequestDispatching(device, fdoContext->Queue, WdfRequestTypeDeviceControl);
        if (!CheckStatus(status)) goto exit;
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "Configured IO queue\n"));

        // Create userspace-accessible device symlink
        status = WdfDeviceCreateSymbolicLink(device, &DeviceSymlink);
        if (!CheckStatus(status)) goto exit;
        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "Created device symlink\n"));
        
        exit:
        return status;
    }

    void EvtDriverCleanup(WDFOBJECT driver) {
        PAGED_CODE();
        UNREFERENCED_PARAMETER(driver);
    }

    NTSTATUS IoCtlInner(WDFDEVICE device, WDFREQUEST request, size_t outputBufferSize, size_t inputBufferSize, size_t& written) {
        if (inputBufferSize < sizeof(int)) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Request too small!\n"));
            return STATUS_UNSUCCESSFUL;
        }

        WDFMEMORY inputMemory;
        if (!CheckStatus(WdfRequestRetrieveInputMemory(request, &inputMemory))) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "Failed to retrieve input memory!\n"));
            return STATUS_UNSUCCESSFUL;
        }

        size_t bufferSize;
        void* inputBuffer = WdfMemoryGetBuffer(inputMemory, &bufferSize);

        if (bufferSize != inputBufferSize) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, DRIVER_LOG_STR "WDF size (%llu B) != buffer size (%llu B)\n", inputBufferSize, bufferSize));
            return STATUS_UNSUCCESSFUL;
        }

        int driverRequest;
        memcpy(&driverRequest, inputBuffer, sizeof(driverRequest));

        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "Received application request %d\n", driverRequest));

        auto status = HandleApplicationRequest(
            device,
            request,
            static_cast<DriverRequestType>(driverRequest),
            {
                .Data = static_cast<BYTE*>(inputBuffer) + sizeof(int),
                .Length =inputBufferSize - sizeof(int)
            },
            outputBufferSize,
            written
        );

        CheckStatus(status);
        return status;
    }

    // bp NVMe_oF_MockDriver!NvmeOFMockDriver::EvtIoQueueIoCtl

    void EvtIoQueueIoCtl(WDFQUEUE queue, WDFREQUEST request, size_t outputBufferSize, size_t inputBufferSize, ULONG ioControlCode) {
        if (ioControlCode != IOCTL_MINIPORT_PROCESS_SERVICE_IRP) {
            KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_WARNING_LEVEL, DRIVER_LOG_STR "Unknown IO Control request, exiting... (0x%x)\n", ioControlCode));
            WdfRequestComplete(request, STATUS_UNSUCCESSFUL);
            return;
        }

        auto device = WdfIoQueueGetDevice(queue);

        size_t written = 0;
        auto result = IoCtlInner(device, request, outputBufferSize, inputBufferSize, written);
        WdfRequestCompleteWithInformation(request, result, written);
    }
}
