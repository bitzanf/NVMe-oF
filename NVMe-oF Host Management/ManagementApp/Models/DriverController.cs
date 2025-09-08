using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KernelInterface;
using ManagementApp.Helpers;

namespace ManagementApp.Models;

/// <summary>
/// MVVM Model for the driver
/// </summary>
internal partial class DriverController : ViewModels.ObservableBase, IDisposable
{
    private IoControlAccess? _driverControl;

    public bool IsConnected => _driverControl != null;

    /// <summary>
    /// Currently configured connections
    /// </summary>
    private ChangeTrackingCollection<Guid, DiskConnectionModel> _connections = [];

    /// <summary>
    /// Newly added connections (never seen by the kernel)
    /// </summary>
    private readonly List<DiskConnectionModel> _newConnections = [];

    /// <summary>
    /// Currently configured connections
    /// </summary>
    public IEnumerable<DiskConnectionModel> Connections => _connections.Values;

    /// <summary>
    /// NVMe Qualified Name of the kernel interface
    /// </summary>
    public string HostNqn
    {
        get => _driverControl?.HostNqn ?? string.Empty;
        set
        {
            ThrowIfNotConnected();
            _driverControl!.HostNqn = value;
        }
    }

    /// <summary>
    /// Get an existing model for the guid or null
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public DiskConnectionModel? TryGetModel(Guid guid) => _connections.GetValueOrDefault(guid);

    /// <summary>
    /// Get the driver's current runtime statistics
    /// </summary>
    /// <returns></returns>
    public Statistics GetDriverStatistics()
    {
        ThrowIfNotConnected();
        return _driverControl!.GetDriverStatistics();
    }

    /// <summary>
    /// Connect to the driver service
    /// </summary>
    public void ConnectToDriver()
    {
        _driverControl = new IoControlAccess();
        OnPropertyChanged(nameof(IsConnected));
    }

    /// <summary>
    /// Disconnect from the driver service
    /// </summary>
    public void DisconnectFromDriver()
    {
        _driverControl?.Dispose();
        _driverControl = null;
        OnPropertyChanged(nameof(IsConnected));
    }

    /// <summary>
    /// Load all connections from the driver
    /// </summary>
    /// <returns></returns>
    public Task LoadConnections() => Task.Run(() =>
    {
        if (_driverControl == null)
            _connections.Clear();
        else
            lock (_driverControl) LoadConnectionsInternal();

        OnPropertyChanged(nameof(Connections));
    });

    /// <summary>
    /// Commit all currently modified connections (hand the changes over to the kernel)
    /// </summary>
    /// <returns></returns>
    private bool CommitChanges()
    {
        if (_driverControl == null) return false;

        lock (_driverControl)
        {
            var changes = _connections.Commit();

            if (changes.Added.Count > 0) throw new Exception("New connections were added through integration, possible data loss!");

            foreach (var removed in changes.Removed) _driverControl.RemoveConnection(removed);
            foreach (var modified in changes.Modified) _driverControl.ModifyConnection(_connections[modified].Descriptor);

            foreach (var added in _newConnections) _driverControl.AddConnection(added.Descriptor);
            _newConnections.Clear();
        }

        return true;
    }

    /// <summary>
    /// Integrate the modified connection models into the kernel
    /// </summary>
    /// <param name="modifiedConnections"></param>
    /// <param name="removedConnections"></param>
    /// <returns></returns>
    public Task<bool> IntegrateChanges(IReadOnlyList<DiskConnectionModel> modifiedConnections, IReadOnlyList<Guid> removedConnections)
        => RunTransaction(() =>
        {
            foreach (var connection in modifiedConnections) IntegrateSingleChangeInternal(connection);
            foreach (var connection in removedConnections) _connections.Remove(connection);
        });

    /// <summary>
    /// Integrate the modified connection model into the kernel
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public Task<bool> IntegrateChanges(DiskConnectionModel connection)
        => RunTransaction(() => IntegrateSingleChangeInternal(connection));

    /// <summary>
    /// Add a new connection (yet unseen by the kernel)
    /// </summary>
    /// <param name="connection"></param>
    /// <returns></returns>
    public Task<bool> AddNewConnection(DiskConnectionModel connection)
        => RunTransaction(() => _newConnections.Add(connection));

    /// <summary>
    /// Remove a given connection
    /// </summary>
    /// <param name="connectionId"></param>
    /// <returns></returns>
    public Task<bool> Remove(Guid connectionId)
        => RunTransaction(() => _connections.Remove(connectionId));

    public void Dispose() => DisconnectFromDriver();

    /// <summary>
    /// Ask the kernel to perform a service discovery on the given remote
    /// </summary>
    /// <param name="discoveryController"></param>
    /// <returns></returns>
    public async Task<List<DiskConnectionModel>> PerformDiscovery(NetworkConnection discoveryController)
    {
        ThrowIfNotConnected();
        var discovered = await _driverControl!.DiscoveryRequest(discoveryController);

        return discovered.Select(disk => new DiskConnectionModel { Descriptor = disk }).ToList();
    }

    /// <summary>
    /// Invokes the given connection-modifying callback, commits the changes and reloads connections from the kernel
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private async Task<bool> RunTransaction(Action callback)
    {
        bool s = await Task.Run(() =>
        {
            if (_driverControl == null) return false;

            lock (_driverControl)
            {
                callback();
                return CommitChanges();
            }
        });

        if (s) await LoadConnections();
        return s;
    }

    /// <summary>
    /// Gets all connections from the kernel and transforms them into connection models
    /// </summary>
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

    /// <summary>
    /// Checks that we are not adding a new connection (only modifications are allowed) and performs the modification
    /// </summary>
    /// <param name="connection"></param>
    /// <exception cref="Exception"></exception>
    private void IntegrateSingleChangeInternal(DiskConnectionModel connection)
    {
        if (connection.Descriptor.Guid == Guid.Empty)
            throw new Exception("New connections must not be integrated this way");

        // The collection actually keeps track of any modifying accesses, so it would set the connection as overwritten
        // ReSharper disable once RedundantCheckBeforeAssignment
        if (_connections[connection.Descriptor.Guid] != connection) _connections[connection.Descriptor.Guid] = connection;
    }

    private void ThrowIfNotConnected()
    {
        if (_driverControl == null) throw new InvalidOperationException("The driver is not connected!");
    }
}