using ManagementApp.Converters;
using ManagementApp.Helpers;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ManagementApp.Views;

/// <summary>
/// View allowing the user to change the driver's NQN or start and stop its service
/// </summary>
public sealed partial class SettingsPage : Page
{
    internal readonly SettingsViewModel ViewModel = new();

    public SettingsPage() => InitializeComponent();

    private void BtnSaveNqn_OnClick(object sender, RoutedEventArgs e)
        => ExceptionToNotificationConverter.WrapExceptions(() =>
        {
            ViewModel.Save();
            NotificationHelper.ShowChangeSaveStatus(true);
        });

    private void BtnCancelNqn_OnClick(object sender, RoutedEventArgs e) => ViewModel.Cancel();

    private void BtnStartService_OnClick(object sender, RoutedEventArgs e)
        => ExceptionToNotificationConverter.WrapExceptions(() =>
        {
            ViewModel.Start();
            ViewModel.Reload();
        });

    private void BtnStopService_OnClick(object sender, RoutedEventArgs e)
        => ExceptionToNotificationConverter.WrapExceptions(() =>
        {
            ViewModel.Stop();
            ViewModel.Reload();
        });

    private void BtnReloadService_OnClick(object sender, RoutedEventArgs e)
        => ExceptionToNotificationConverter.WrapExceptions(ViewModel.Reload);
}