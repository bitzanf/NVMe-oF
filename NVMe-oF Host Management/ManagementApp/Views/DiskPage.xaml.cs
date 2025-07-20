using System.Collections.Generic;
using ManagementApp.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static ManagementApp.Models.DiskConnectionModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DiskPage : Page
{
    internal ViewModels.DiskListViewModel ViewModel = new();

    public DiskPage()
    {
        InitializeComponent();

        Loaded += async (_, _) => await ViewModel.LoadConnections();
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.LoadConnections();
}