using KernelInterface;

namespace ManagementApp.ViewModels;

internal class StatsViewModel : ObservableBase
{
    private Statistics? _statistics;

    public float PacketsPerSecond => GetValueOrDefault().PacketsPerSecond;

    public uint AverageRequestSize => GetValueOrDefault().AverageRequestSize;

    public ulong TotalDataTransferred => GetValueOrDefault().TotalDataTransferred;

    public void Reload()
    {
        _statistics = App.DriverController.GetDriverStatistics();
    }

    private Statistics GetValueOrDefault() => _statistics.GetValueOrDefault();
}