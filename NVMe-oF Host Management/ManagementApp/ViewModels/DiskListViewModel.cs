using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using ManagementApp.Models;

namespace ManagementApp.ViewModels;

/// <summary>
/// MVVM ViewModel representing a list of all connections, tracks modifications and removals
/// </summary>
internal partial class DiskListViewModel : ObservableBase
{
    private readonly DispatcherQueue _queue;
    private bool _isLoading, _suppressConnectionsUpdate, _hasChanges;

    private readonly List<Guid> _removedConnections = [];

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetField(ref _isLoading, value);
            OnPropertyChanged(nameof(KnownNoDisks));
            OnPropertyChanged(nameof(CanRefresh));
        }
    }

    public bool CanRefresh => !IsLoading && App.DriverController.IsConnected;

    public bool HasChanges
    {
        get => _hasChanges;
        set
        {
            if (!value) _removedConnections.Clear();
            SetField(ref _hasChanges, value);
        }
    }

    public bool KnownNoDisks => !IsLoading && Connections.Count == 0;

    public ObservableCollection<DiskConnectionModel> Connections = [];

    public IReadOnlyList<Guid> RemovedConnections => _removedConnections;

    public DiskListViewModel()
    {
        IsLoading = false;
        HasChanges = false;

        _queue = DispatcherQueue.GetForCurrentThread();

        _suppressConnectionsUpdate = false;
        Connections.CollectionChanged += Connections_CollectionChanged;
    }

    private void Connections_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_suppressConnectionsUpdate) return;

        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(KnownNoDisks));
    }

    /// <summary>
    /// Load the connections from the driver controller and populate the display collection
    /// </summary>
    /// <returns></returns>
    public async Task LoadConnections()
    {
        await _queue.EnqueueAsync(() =>
        {
            IsLoading = true;
            Connections.Clear();
            _removedConnections.Clear();
        });

        var connections = App.DriverController.Connections;

         await _queue.EnqueueAsync(() =>
         {
             try
             {
                 _suppressConnectionsUpdate = true;
                 foreach (var connection in connections) Connections.Add(connection);
             }
             finally
             {
                 _suppressConnectionsUpdate = false;
             }

             IsLoading = false;
             HasChanges = false;
         });
    }

    /// <summary>
    /// Force the driver controller to reload all connections from the kernel and calls <see cref="LoadConnections"/>
    /// </summary>
    /// <returns></returns>
    public async Task ForceReload()
    {
        await _queue.EnqueueAsync(() => IsLoading = true);
        await App.DriverController.LoadConnections();
        await LoadConnections();
    }

    /// <summary>
    /// Delete the given connection
    /// </summary>
    /// <param name="connection"></param>
    public void DeleteConnection(DiskConnectionModel connection)
    {
        Connections.Remove(connection);
        _removedConnections.Add(connection.Descriptor.Guid);
        HasChanges = true;
    }
}