using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using ManagementApp.Models;

namespace ManagementApp.ViewModels;

internal partial class DiskListViewModel : ObservableBase
{
    private readonly DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();
    private bool _isLoading, _suppressConnectionsUpdate, _hasChanges;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            SetField(ref _isLoading, value);
            OnPropertyChanged(nameof(KnownNoDisks));
        }
    }

    public bool HasChanges
    {
        get => _hasChanges;
        set => SetField(ref _hasChanges, value);
    }

    public bool KnownNoDisks => !IsLoading && Connections.Count == 0;

    public ObservableCollection<DiskConnectionModel> Connections = [];

    public DiskListViewModel()
    {
        IsLoading = false;
        HasChanges = false;

        _suppressConnectionsUpdate = false;
        Connections.CollectionChanged += Connections_CollectionChanged;
    }

    private void Connections_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (_suppressConnectionsUpdate) return;

        OnPropertyChanged(nameof(Connections));
        OnPropertyChanged(nameof(KnownNoDisks));
    }

    public async Task LoadConnections()
    {
        await _queue.EnqueueAsync(() =>
        {
            IsLoading = true;
            Connections.Clear();
        });

         var connections = await App.DriverController.LoadConnections();

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
         });
    }

    public void DeleteConnection(DiskConnectionModel connection)
    {
        Connections.Remove(connection);
        HasChanges = true;
    }
}