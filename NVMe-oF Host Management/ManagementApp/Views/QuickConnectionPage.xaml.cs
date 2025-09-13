using CommunityToolkit.WinUI.Behaviors;
using ManagementApp.Converters;
using ManagementApp.Helpers;
using ManagementApp.Models;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;

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

            if (!Validate()) return;

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

    /// <summary>
    /// Simple validation of the given data; validation errors get shown as a notification
    /// </summary>
    /// <returns></returns>
    private bool Validate()
    {
        var loader = ResourceLoader.GetForViewIndependentUse();

        var controller = ViewModel.DiscoveryController;
        List<string> validationErrors = [];

        if (string.IsNullOrWhiteSpace(controller.TransportAddress))
            validationErrors.Add(loader.GetString("ValidationError_Address"));

        if (controller.TransportServiceId == 0)
            validationErrors.Add(loader.GetString("ValidationError_Port"));

        if (validationErrors.Count <= 0) return true;

        Notification notification = new()
        {
            Severity = InfoBarSeverity.Error,
            Title = loader.GetString("ValidationError_Msg_Title"),
            Message = string.Join('\n', validationErrors)
        };

        MainWindow.Instance!.ShowNotification(notification);

        return false;
    }
}