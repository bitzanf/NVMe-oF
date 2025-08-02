using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ManagementApp.Converters;
using ManagementApp.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatsPage : Page
{
    internal StatsViewModel ViewModel = new();

    public bool IsConnected => App.DriverController.IsConnected;

    public StatsPage()
    {
        InitializeComponent();
        Reload();
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => Reload();

    private void Reload() => ExceptionToNotificationConverter.WrapExceptions(ViewModel.Reload);
}