using System;
using System.Linq;
using System.ServiceProcess;
using Windows.ApplicationModel.Resources;

namespace ManagementApp.Helpers;

/// <summary>
/// Controls the driver's service, effectively activating or deactivating the driver
/// </summary>
public class DriverServiceHelper
{
    /// <summary>
    /// Name of the service
    /// </summary>
    private const string ServiceName = "NVMe-oF_MockDriver";

    /// <summary>
    /// Service instance to control
    /// </summary>
    private ServiceController? _device = GetService();

    /// <summary>
    /// Does the service exist at all?
    /// </summary>
    public bool IsPresent => _device != null;

    /// <summary>
    /// Is the service currently running (is the driver active &amp; loaded)?
    /// </summary>
    public bool IsRunning
    {
        get
        {
            ThrowIfNoDevice();
            return _device!.Status == ServiceControllerStatus.Running;
        }
    }

    /// <summary>
    /// Is the service currently changing states?
    /// </summary>
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

    /// <summary>
    /// Start the service (activate the driver)
    /// </summary>
    public void Start()
    {
        ThrowIfNoDevice();
        _device?.Start();
    }

    /// <summary>
    /// Stop the service (deactivate the driver)
    /// </summary>
    public void Stop()
    {
        ThrowIfNoDevice();
        _device?.Stop();
    }

    /// <summary>
    /// Refresh the service's instance
    /// </summary>
    public void Reload() => _device = GetService();

    /// <summary>
    /// Throw if the service is not present at all
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    private void ThrowIfNoDevice()
    {
        if (IsPresent) return;
        
        var loader = ResourceLoader.GetForViewIndependentUse();

        var msg = loader.GetString("DeviceNotPresent");
        if (string.IsNullOrEmpty(msg)) msg = "Device not present!";

        throw new InvalidOperationException(msg);
    }

    /// <summary>
    /// Load the service's instance from the system
    /// </summary>
    /// <returns></returns>
    private static ServiceController? GetService()
    {
        var devices = ServiceController.GetDevices();
        return devices.FirstOrDefault(d => d.ServiceName == ServiceName);
    }
}