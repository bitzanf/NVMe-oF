using System.Collections.Generic;
using System.Threading.Tasks;
using KernelInterface;
using static ManagementApp.Models.DiskConnectionModel;

namespace ManagementApp.Models;

internal partial class DriverController : ViewModels.ObservableBase
{
    private IDriverControl? _driverControl = null;

    public bool IsConnected => _driverControl != null;

    public void ConnectToDriver(string devicePath)
    {
        _driverControl = new IoControlAccess(devicePath);
        OnPropertyChanged(nameof(IsConnected));
    }

    public void DisconnectFromDriver()
    {
        _driverControl = null;
        OnPropertyChanged(nameof(IsConnected));
    }

    public async Task<List<DiskConnectionModel>> LoadConnections()
    {
        return await Task.Run(() => new List<DiskConnectionModel> {
            new()
            {
                ConnectionStatus = ConnectionStatusEnum.Connected,
                Nqn = "zfs-netdisk",
                TransportAddress = "10.1.0.50",
                TransportServiceId = 4420,
                AddressFamily = AddressFamilyEnum.IPv4,
                TransportType = TransportTypeEnum.Tcp,
                NtObjectPath = @"\Disks\VirtualDisk0"
            },
            new()
            {
                ConnectionStatus = ConnectionStatusEnum.Disconnected,
                Nqn = "nqn.2014-08.org.meow.disks",
                TransportAddress = "fe80::c2b3:ea32:35e1:c4d6%22",
                TransportServiceId = 12345,
                AddressFamily = AddressFamilyEnum.IPv6,
                TransportType = TransportTypeEnum.Rdma,
                NtObjectPath = @"\Disks\VirtualDisk1"
            },
            new()
            {
                ConnectionStatus = ConnectionStatusEnum.Connecting,
                Nqn = "nqn.2014-08.org.nvmexpress.discovery",
                TransportAddress = "75.209.63.155",
                TransportServiceId = 4420,
                AddressFamily = AddressFamilyEnum.IPv4,
                TransportType = TransportTypeEnum.Tcp,
                NtObjectPath = @"\Disks\VirtualDisk2"
            },
        });
    }
}