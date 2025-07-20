using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ManagementApp.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DiskPage : Page
{
    internal List<DiskConnectionModel> Connections;

    public DiskPage()
    {
        InitializeComponent();

        Connections = [
            new DiskConnectionModel {
                ConnectionStatus = DiskConnectionModel.ConnectionStatusEnum.Connected,
                Nqn = "zfs-netdisk",
                TransportAddress = "10.1.0.50",
                TransportServiceId = 4420,
                AddressFamily = DiskConnectionModel.AddressFamilyEnum.IPv4,
                TransportType = DiskConnectionModel.TransportTypeEnum.Tcp,
                NtObjectPath = @"\Disks\VirtualDisk0"
            },
            new DiskConnectionModel {
                ConnectionStatus = DiskConnectionModel.ConnectionStatusEnum.Disconnected,
                Nqn = "nqn.2014-08.org.meow.disks",
                TransportAddress = "fe80::c2b3:ea32:35e1:c4d6%22",
                TransportServiceId = 12345,
                AddressFamily = DiskConnectionModel.AddressFamilyEnum.IPv6,
                TransportType = DiskConnectionModel.TransportTypeEnum.Rdma,
                NtObjectPath = @"\Disks\VirtualDisk1"
            },
            new DiskConnectionModel {
                ConnectionStatus = DiskConnectionModel.ConnectionStatusEnum.Connecting,
                Nqn = "nqn.2014-08.org.nvmexpress.discovery",
                TransportAddress = "75.209.63.155",
                TransportServiceId = 4420,
                AddressFamily = DiskConnectionModel.AddressFamilyEnum.IPv4,
                TransportType = DiskConnectionModel.TransportTypeEnum.Tcp,
                NtObjectPath = @"\Disks\VirtualDisk2"
            },
        ];
    }
}