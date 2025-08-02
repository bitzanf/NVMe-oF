using System.Collections.ObjectModel;
using KernelInterface;
using ManagementApp.Models;

namespace ManagementApp.ViewModels;

internal class QuickConnectViewModel : ObservableBase
{
    public NetworkConnection DiscoveryController { get; init; } = new();

    public ObservableCollection<DiskConnectionModel> Connections { get; private set; } = [];
}