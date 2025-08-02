using Windows.ApplicationModel.Resources;
using ManagementApp.Converters;
using ManagementApp.Helpers;

namespace ManagementApp.ViewModels;

internal class SettingsViewModel : ObservableBase
{
    private readonly DriverServiceHelper _driverServiceHelper;

    private string _nqn, _serviceStatus;

    public SettingsViewModel()
    {
        _nqn = App.DriverController.HostNqn;
        _serviceStatus = string.Empty;
        _driverServiceHelper = new();

        ExceptionToNotificationConverter.WrapExceptions(Reload);
    }

    public string Nqn
    {
        get => _nqn;
        set => SetField(ref _nqn, value);
    }

    public string ServiceStatus
    {
        get => _serviceStatus;
        set => SetField(ref _serviceStatus, value);
    }

    public void Start()
    {
        if (_driverServiceHelper is { IsRunning: false, IsUpdating: false })
            _driverServiceHelper.Start();
    }

    public void Stop()
    {
        if (_driverServiceHelper is { IsRunning: true, IsUpdating: false })
            _driverServiceHelper.Stop();
    }

    public void Save() => App.DriverController.HostNqn = Nqn;

    public void Cancel() => Nqn = App.DriverController.HostNqn;

    public void Reload()
    {
        _driverServiceHelper.Reload();

        var loader = ResourceLoader.GetForViewIndependentUse();
        if (_driverServiceHelper.IsUpdating) ServiceStatus = loader.GetString("ServiceStatus_Updating");
        else if (_driverServiceHelper.IsRunning) ServiceStatus = loader.GetString("ServiceStatus_Running");
        else ServiceStatus = loader.GetString("ServiceStatus_Stopped");
    }
}