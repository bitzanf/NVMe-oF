using System;
using System.Linq;
using System.ServiceProcess;
using Windows.ApplicationModel.Resources;

namespace ManagementApp.Helpers;

public class DriverServiceHelper
{
    private const string ServiceName = "NVMe-oF_MockDriver";

    private ServiceController? _device = GetService();

    public bool IsPresent => _device != null;

    public bool IsRunning
    {
        get
        {
            ThrowIfNoDevice();
            return _device!.Status == ServiceControllerStatus.Running;
        }
    }

    public bool IsUpdating
    {
        get
        {
            ThrowIfNoDevice();
            return _device!.Status switch
            {
                ServiceControllerStatus.StartPending
                    or ServiceControllerStatus.StopPending
                    or ServiceControllerStatus.ContinuePending => true,

                _ => false
            };
        }
    }

    public void Start()
    {
        ThrowIfNoDevice();
        _device?.Start();
    }

    public void Stop()
    {
        ThrowIfNoDevice();
        _device?.Stop();
    }

    public void Reload() => _device = GetService();

    private void ThrowIfNoDevice()
    {
        if (IsPresent) return;
        
        var loader = ResourceLoader.GetForViewIndependentUse();

        var msg = loader.GetString("DeviceNotPresent");
        if (string.IsNullOrEmpty(msg)) msg = "Device not present!";

        throw new InvalidOperationException(msg);
    }

    private static ServiceController? GetService()
    {
        var devices = ServiceController.GetDevices();
        return devices.FirstOrDefault(d => d.ServiceName == ServiceName);
    }
}