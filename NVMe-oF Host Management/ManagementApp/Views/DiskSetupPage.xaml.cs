using ManagementApp.Models;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Media.Animation;

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

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.ForceReload();

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        App.DriverController.IntegrateChanges(ViewModel.Connections.ToList());
        ViewModel.HasChanges = false;

        // TODO: Localize
        Notification notification = new()
        {
            Message = "Changes saved successfully.",
            Severity = InfoBarSeverity.Success,
            Duration = TimeSpan.FromSeconds(5)
        };
        MainWindow.Instance!.ShowNotification(notification);
    }

    private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }

    private async void DeleteCommandCallback(DiskConnectionModel model)
    {
        var result = await ConfirmDeleteDialog.ShowDialog(XamlRoot, model);

        if (result == ContentDialogResult.Primary) ViewModel.DeleteConnection(model);
    }

    private async void EditCommandCallback(DiskConnectionModel model)
    {
        // Check that we have no unsaved changes (or are ok with losing them)
        var canNavigate = await RemindUnsavedChanges_IsExitOk();
        if (canNavigate)
            Frame.Navigate(typeof(DiskEditPage), model.Guid, new EntranceNavigationTransitionInfo());
    }

    private void BtnAdd_OnClick(object sender, RoutedEventArgs e)
    {
        // TODO: Navigate to edit form, pass null as parameter
        //  set App.DriverController.NewlyConfiguredModel to null to prevent stale preconfigured data from showing in the dialog
        throw new NotImplementedException();
    }

    private async Task<bool> RemindUnsavedChanges_IsExitOk()
    {
        // TODO: Actually implement a proper dialog
        var disk = ViewModel.Connections.FirstOrDefault();
        if (disk == null) return true;

        var result = await ConfirmDeleteDialog.ShowDialog(XamlRoot, disk);
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
