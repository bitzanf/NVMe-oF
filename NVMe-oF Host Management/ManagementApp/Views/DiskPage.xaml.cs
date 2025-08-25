using ManagementApp.Converters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ManagementApp.Views;

/// <summary>
/// View presenting the user with a list of all connections
/// </summary>
public sealed partial class DiskPage : Page
{
    internal ViewModels.DiskListViewModel ViewModel = new();

    public DiskPage()
    {
        InitializeComponent();

        Loaded += async (_, _) => await ViewModel.LoadConnections();
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(ViewModel.ForceReload);
}