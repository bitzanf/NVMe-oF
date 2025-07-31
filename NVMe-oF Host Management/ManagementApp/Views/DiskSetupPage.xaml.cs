using ManagementApp.Models;
using ManagementApp.ViewModels;
using ManagementApp.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ManagementApp.Converters;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class DiskSetupPage : Page
{
    internal DiskListViewModel ViewModel = new();

    public ICommand DeleteCommand => new DiskCommandEventHandler(DeleteCommandCallback);
    public ICommand EditCommand => new DiskCommandEventHandler(EditCommandCallback);

    public DiskSetupPage()
    {
        InitializeComponent();

        Loaded += async (_, _) => await ViewModel.LoadConnections();

        Loaded += (_, _) => MainWindow.Instance!.OnNavigationRequested += RemindUnsavedChanges_IsExitOk;
        Unloaded += (_, _) => MainWindow.Instance!.OnNavigationRequested -= RemindUnsavedChanges_IsExitOk;
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e) 
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            var discard = await RemindUnsavedChanges_IsExitOk();
            if (discard) await ViewModel.ForceReload();
        });

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        App.DriverController.IntegrateChanges(ViewModel.Connections.ToList());
        ViewModel.HasChanges = false;

        NotificationHelper.ShowChangeSaveStatus(App.DriverController.CommitChanges());
    }

    private async void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(ViewModel.LoadConnections);

    private async void DeleteCommandCallback(DiskConnectionModel model)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            var result = await ConfirmDeleteDialog.ShowDialog(XamlRoot, model);

            if (result == ContentDialogResult.Primary) ViewModel.DeleteConnection(model);
        });

    private async void EditCommandCallback(DiskConnectionModel model)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            // Check that we have no unsaved changes (or are ok with losing them)
            var canNavigate = await RemindUnsavedChanges_IsExitOk();
            if (canNavigate)
                Frame.Navigate(typeof(DiskEditPage), model.Descriptor.Guid, new EntranceNavigationTransitionInfo());
        });

    private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(DiskEditPage), null, new EntranceNavigationTransitionInfo());
    }

    private async Task<bool> RemindUnsavedChanges_IsExitOk()
    {
        if (!ViewModel.HasChanges) return true;

        var result = await UnsavedChangesDialog.ShowDialog(XamlRoot);
        return result == ContentDialogResult.Primary;
    }
}
