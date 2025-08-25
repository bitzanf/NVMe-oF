using ManagementApp.Converters;
using ManagementApp.Models;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Windows.Input;
using ManagementApp.Helpers;

namespace ManagementApp.Views;

/// <summary>
/// View that allows the user to perform discovery requests and quickly connect to discovered disks
/// </summary>
public sealed partial class QuickConnectionPage : Page
{
    internal QuickConnectViewModel ViewModel = new();

    public ICommand ConnectCommand => new DiskCommandEventHandler(QuickConnectCommandCallback);

    public QuickConnectionPage() => InitializeComponent();

    /// <summary>
    /// Connect to the given discovery controller and request all possible connections
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BtnConnect_OnClick(object sender, RoutedEventArgs e)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            if (ViewModel.IsLoading) return;

            ViewModel.IsLoading = true;
            var discovered = await App.DriverController.PerformDiscovery(ViewModel.DiscoveryController);
            foreach (var model in discovered) ViewModel.Connections.Add(model);
            ViewModel.IsLoading = false;
        });

    /// <summary>
    /// Connect to a selected remote disk
    /// </summary>
    /// <param name="model"></param>
    private async void QuickConnectCommandCallback(DiskConnectionModel model)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            if (ViewModel.IsLoading) return;

            bool success = await App.DriverController.AddNewConnection(model);
            if (success) ViewModel.Connections.Remove(model);

            NotificationHelper.ShowChangeSaveStatus(success);
        });
}