using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KernelInterface;
using ManagementApp.Helpers;

namespace ManagementApp.Models;

internal partial class DriverController : ViewModels.ObservableBase, IDisposable
{
    private IoControlAccess? _driverControl;

    public const string DevicePath = @"\\.\NvmeOfController";

    public bool IsConnected => _driverControl != null;

    private ChangeTrackingCollection<Guid, DiskConnectionModel> _connections = [];

    public IEnumerable<DiskConnectionModel> Connections => _connections.Values;

    public string HostNqn
    {
        get => _driverControl?.HostNqn ?? string.Empty;
        set
        {
            ThrowIfNotConnected();
            _driverControl!.HostNqn = value;
        }
    }

    public DiskConnectionModel? TryGetModel(Guid guid) => _connections.GetValueOrDefault(guid);

    public Statistics GetDriverStatistics()
    {
        ThrowIfNotConnected();
        return _driverControl!.GetDriverStatistics();
    }

    public void ConnectToDriver(string devicePath)
    {
        _driverControl = new IoControlAccess(devicePath);
        OnPropertyChanged(nameof(IsConnected));
    }

    public void DisconnectFromDriver()
    {
        _driverControl?.Dispose();
        _driverControl = null;
        OnPropertyChanged(nameof(IsConnected));
    }

    public Task LoadConnections() => Task.Run(() =>
    {
        if (_driverControl == null)
            _connections.Clear();
        else
            lock (_driverControl) LoadConnectionsInternal();

        OnPropertyChanged(nameof(Connections));
    });

    private bool CommitChanges()
    {
        if (_driverControl == null) return false;

        lock (_driverControl)
        {
            var changes = _connections.Commit();

            foreach (var removed in changes.Removed) _driverControl.RemoveConnection(removed);
            foreach (var modified in changes.Modified) _driverControl.ModifyConnection(_connections[modified].Descriptor);
            foreach (var added in changes.Added) _driverControl.AddConnection(_connections[added].Descriptor);
        }

        return true;
    }

    public Task<bool> IntegrateChanges(IReadOnlyList<DiskConnectionModel> connections)
        => RunTransaction(() =>
        {
            foreach (var connection in connections)
                _connections.Add(connection.Descriptor.Guid, connection);
        });

    public Task<bool> IntegrateChanges(DiskConnectionModel connection)
        => RunTransaction(() => _connections.Add(connection.Descriptor.Guid, connection));

    public Task<bool> Remove(Guid connectionId)
        => RunTransaction(() => _connections.Remove(connectionId));

    public void Dispose() => DisconnectFromDriver();

    public async Task<List<DiskConnectionModel>> PerformDiscovery(NetworkConnection discoveryController)
    {
        ThrowIfNotConnected();
        var discovered = await _driverControl!.DiscoveryRequest(discoveryController);

        return discovered.Select(disk => new DiskConnectionModel { Descriptor = disk }).ToList();
    }

    private Task<bool> RunTransaction(Action callback)
        => Task.Run(() =>
        {
            if (_driverControl == null) return false;

            lock (_driverControl)
            {
                callback();
                var success = CommitChanges();
                OnPropertyChanged(nameof(Connections));
                return success;
            }
        });

    private void LoadConnectionsInternal()
    {
        var connections = _driverControl!.GetConfiguredConnections();

        var models = connections.Select(d => new DiskConnectionModel
        {
            Descriptor = d,
            ConnectionStatus = _driverControl!.GetConnectionStatus(d.Guid)
        });

        _connections = new (models.Select(m => new KeyValuePair<Guid, DiskConnectionModel>(m.Descriptor.Guid, m)));
    }

    private void ThrowIfNotConnected()
    {
        if (_driverControl == null) throw new InvalidOperationException("The driver is not connected!");
    }
}