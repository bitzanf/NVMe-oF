#include <ntddk.h>
#include <wdf.h>

#define DRIVER_LOG_STR "Mock NVMe-oF Driver: "

const UNICODE_STRING DeviceSymlink = RTL_CONSTANT_STRING(L"\\??\\NvmeOfController");

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

NTSTATUS DriverEntry(PDRIVER_OBJECT driverObject,PUNICODE_STRING registryPath) {
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

    void EvtIoQueueIoCtl(WDFQUEUE queue, WDFREQUEST request, size_t outputBufferSize, size_t inputBufferSize, ULONG ioControlCode) {
        UNREFERENCED_PARAMETER(queue);
        UNREFERENCED_PARAMETER(request);
        UNREFERENCED_PARAMETER(outputBufferSize);
        UNREFERENCED_PARAMETER(inputBufferSize);

        KdPrintEx((DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, DRIVER_LOG_STR "IO Control Request: 0x%x\n", ioControlCode));

        WdfRequestComplete(request, STATUS_SUCCESS);
    }
}
