using System.Collections.ObjectModel;
using KernelInterface;
using ManagementApp.Models;

namespace ManagementApp.ViewModels;

internal class QuickConnectViewModel : ObservableBase
{
    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    public NetworkConnection DiscoveryController { get; init; } = new();

    public ObservableCollection<DiskConnectionModel> Connections { get; private set; } = [];
}