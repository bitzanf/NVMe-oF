using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KernelInterface;
using ManagementApp.Models;

namespace ManagementApp.ViewModels;

internal class QuickConnectViewModel : ObservableBase
{
    public NetworkConnection DiscoveryController { get; init; } = new();

    public ObservableCollection<DiskConnectionModel> Connections { get; private set; } = [];
}