using ManagementApp.Models;
using ManagementApp.ViewModels;
using ManagementApp.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System.Threading.Tasks;
using System.Windows.Input;
using ManagementApp.Converters;
using ManagementApp.Dialogs;

namespace ManagementApp.Views;

/// <summary>
/// View that allows the user to add, modify or delete existing connections
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

    /// <summary>
    /// Reminds the user about unsaved changes and reloads all connections from the kernel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e) 
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            var discard = await RemindUnsavedChanges_IsExitOk();
            if (discard) await ViewModel.ForceReload();
        });

    /// <summary>
    /// Integrates the current changes (modify &amp; remove) into the kernel
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BtnSave_OnClick(object sender, RoutedEventArgs e)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            bool success = await App.DriverController.IntegrateChanges(ViewModel.Connections, ViewModel.RemovedConnections);
            ViewModel.HasChanges = false;

            NotificationHelper.ShowChangeSaveStatus(success);
        });

    /// <summary>
    /// Discard all changes and reload connections
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(ViewModel.LoadConnections);

    /// <summary>
    /// Ask for confirmation and remove the connection
    /// </summary>
    /// <param name="model"></param>
    private async void DeleteCommandCallback(DiskConnectionModel model)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            var result = await ConfirmDeleteDialog.ShowDialog(XamlRoot, model);

            if (result == ContentDialogResult.Primary) ViewModel.DeleteConnection(model);
        });

    /// <summary>
    /// Check for unsaved changes and transfer the user to a modification view for the selected model
    /// </summary>
    /// <param name="model"></param>
    private async void EditCommandCallback(DiskConnectionModel model)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            // Check that we have no unsaved changes (or are ok with losing them)
            var canNavigate = await RemindUnsavedChanges_IsExitOk();
            if (canNavigate)
                Frame.Navigate(typeof(DiskEditPage), model.Descriptor.Guid, new EntranceNavigationTransitionInfo());
        });

    /// <summary>
    /// Transfer the user to a blank modification view
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
        => Frame.Navigate(typeof(DiskEditPage), null, new EntranceNavigationTransitionInfo());

    /// <summary>
    /// Present the user with a dialog warning them about unsaved changes
    /// </summary>
    /// <returns></returns>
    private async Task<bool> RemindUnsavedChanges_IsExitOk()
    {
        if (!ViewModel.HasChanges) return true;
        return await SimpleMessageDialogs.UnsavedChanges(XamlRoot);
    }
}
