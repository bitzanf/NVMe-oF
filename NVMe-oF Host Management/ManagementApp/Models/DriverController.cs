using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KernelInterface;

namespace ManagementApp.Models;

internal partial class DriverController : ViewModels.ObservableBase
{
    private IDriverControl? _driverControl = null;

    public bool IsConnected => _driverControl != null;

    public List<DiskConnectionModel> Connections { get; private set; } = [];

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

    public Task LoadConnections() => Task.Run(() =>
    {
        if (_driverControl == null)
        {
            // TODO: Remove ugly hack
            //Connections.Clear();
            LoadConnectionsInternal();
            return;
        }

        lock (_driverControl) LoadConnectionsInternal();
    });

    public bool CommitChanges()
    {
        // TODO: is this the best API choice?
        //  the process is *always* Integrate(); Commit();

        if (_driverControl == null) return false;

        lock (_driverControl)
        {
            var connections = Connections;
            LoadConnectionsInternal();
        }

        return true;
    }

    public void IntegrateChanges(IReadOnlyList<DiskConnectionModel> connections)
    {
        // TODO: is this a good idea...?
        Connections = connections.ToList();
    }

    public void IntegrateChanges(DiskConnectionModel connection)
    {
        // TODO: probably some kind of audit log for easier kernel commits...
        //  Also detect changes vs newly configured connections...
        Connections.Add(connection);
    }

    private void LoadConnectionsInternal()
    {
        // TODO: Actually ask the driver for connections...
        Connections =
        [
            new()
            {
                ConnectionStatus = ConnectionStatus.Connected,
                Descriptor = new()
                {
                    Nqn = "zfs-netdisk",
                    NetworkConnection = new()
                    {
                        TransportAddress = "10.1.0.50",
                        TransportServiceId = 4420,
                        AddressFamily = AddressFamily.IPv4,
                        TransportType = TransportType.Tcp,
                    },
                    NtObjectPath = @"\Disks\VirtualDisk0",
                    Guid = Guid.NewGuid()
                }
            },
            new()
            {
                ConnectionStatus = ConnectionStatus.Disconnected,
                Descriptor = new()
                {
                    Nqn = "nqn.2014-08.org.meow.disks",
                    NetworkConnection = new()
                    {
                        TransportAddress = "fe80::c2b3:ea32:35e1:c4d6%22",
                        TransportServiceId = 12345,
                        AddressFamily = AddressFamily.IPv6,
                        TransportType = TransportType.Rdma,
                    },
                    NtObjectPath = @"\Disks\VirtualDisk1",
                    Guid = Guid.NewGuid()
                }
            },
            new()
            {
                ConnectionStatus = ConnectionStatus.Connecting,
                Descriptor = new()
                {
                    Nqn = "nqn.2014-08.org.nvmexpress.discovery",
                    NetworkConnection = new()
                    {
                        TransportAddress = "75.209.63.155",
                        TransportServiceId = 4420,
                        AddressFamily = AddressFamily.IPv4,
                        TransportType = TransportType.Tcp,
                    },
                    NtObjectPath = @"\Disks\VirtualDisk2",
                    Guid = Guid.NewGuid()
                }
            }
        ];
    }
}