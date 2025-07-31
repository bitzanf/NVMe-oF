using ManagementApp.Converters;
using ManagementApp.Models;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Windows.Input;
using ManagementApp.Helpers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class QuickConnectionPage : Page
{
    internal QuickConnectViewModel ViewModel = new();

    public ICommand ConnectCommand => new DiskCommandEventHandler(QuickConnectCommandCallback);

    public QuickConnectionPage()
    {
        InitializeComponent();
    }

    private async void BtnConnect_OnClick(object sender, RoutedEventArgs e)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            // TODO: pass network info to driver controller for service discovery
            
            ViewModel.Connections.Add(App.DriverController.Connections[1]);
        });

    private async void QuickConnectCommandCallback(DiskConnectionModel model)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            // TODO: pass the model to the driver as a newly configured connection
            throw new NotImplementedException();
        });
}