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
    }

    private async void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => await ViewModel.LoadConnections();

    private void BtnSave_OnClick(object sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
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

    private void EditCommandCallback(DiskConnectionModel model)
    {

    }

    // TODO FIX THIS
    protected override async void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        //if (ViewModel.HasChanges)
        if (new Random().Next(2) == 0)
        {
            e.Cancel = true;
            var result = await ConfirmDeleteDialog.ShowDialog(XamlRoot, ViewModel.Connections.First());
            if (result == ContentDialogResult.Primary)
            {
                e.Cancel = false;
                Resume();
            }
            else
            {
                await ViewModel.LoadConnections();
            }
        }

        base.OnNavigatingFrom(e);
        return;

        void Resume()
        {
            if (e.NavigationMode == NavigationMode.Back) Frame.GoBack();
            else Frame.Navigate(e.SourcePageType, e.Parameter, e.NavigationTransitionInfo);
        }
    }

    internal class CommandEventHandler(Action<DiskConnectionModel> action) : ICommand
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
