using CommunityToolkit.WinUI.Behaviors;
using ManagementApp.Models;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel.Resources;
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

    public ICommand DeleteCommand => new CommandEventHandler(DeleteCommandCallback);
    public ICommand EditCommand => new CommandEventHandler(EditCommandCallback);

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

        var loader = ResourceLoader.GetForViewIndependentUse();

        Notification notification = App.DriverController.CommitChanges()
            ? new()
            {
                Title = loader.GetString("ChangesSavedNotification_Success"),
                Duration = TimeSpan.FromSeconds(5),
                Severity = InfoBarSeverity.Success
            }
            : new()
            {
                Title = loader.GetString("ChangesSavedNotification_Error"),
                Message = loader.GetString("ChangesSavedNotification_ErrorMessage"),
                Duration = TimeSpan.FromSeconds(15),
                Severity = InfoBarSeverity.Error
            };

        MainWindow.Instance!.ShowNotification(notification);
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
        //  set to null to prevent stale preconfigured data from showing in the dialog
        App.DriverController.NewlyConfiguredModel = null;
        Frame.Navigate(typeof(DiskEditPage), null, new EntranceNavigationTransitionInfo());
    }

    private async Task<bool> RemindUnsavedChanges_IsExitOk()
    {
        if (!ViewModel.HasChanges) return true;

        var result = await UnsavedChangesDialog.ShowDialog(XamlRoot);
        return result == ContentDialogResult.Primary;
    }

    private class CommandEventHandler(Action<DiskConnectionModel> action) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            if (parameter is not DiskConnectionModel model) return;

            action(model);
        }
    }
}
