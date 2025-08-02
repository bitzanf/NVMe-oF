using CommunityToolkit.WinUI.Behaviors;
using ManagementApp.Helpers;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using ManagementApp.Dialogs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DiskEditPage : Page
    {
        internal DiskEditViewModel ViewModel = null!;

        public bool ShowInfo => !string.IsNullOrEmpty(ViewModel?.NtObjectPath);

        public DiskEditPage()
        {
            InitializeComponent();

            Loaded += (_, _) => MainWindow.Instance!.OnNavigationRequested += RemindUnsavedChanges_IsExitOk;
            Unloaded += (_, _) => MainWindow.Instance!.OnNavigationRequested -= RemindUnsavedChanges_IsExitOk;
        }

        // https://learn.microsoft.com/en-us/windows/apps/design/basics/navigate-between-two-pages
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            switch (e.Parameter)
            {
                case null:
                    // We are requested to create a new connection
                    ViewModel = new(null);
                    break;

                case Guid guid:
                    LoadViewModel(guid);
                    break;
            }

            base.OnNavigatedTo(e);
        }

        private void BtnSave_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            App.DriverController.IntegrateChanges(ViewModel.Model);
            NotificationHelper.ShowChangeSaveStatus(App.DriverController.CommitChanges());

            // We've edited and saved the disk, return to previous page (likely DiskSetupPage)
            Frame.GoBack();
        }

        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        private bool Validate()
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            var descriptor = ViewModel.Model.Descriptor;
            List<string> validationErrors = [];


            if (string.IsNullOrWhiteSpace(descriptor.Nqn))
                validationErrors.Add(loader.GetString("ValidationError_Nqn"));
            
            if (string.IsNullOrWhiteSpace(descriptor.NetworkConnection.TransportAddress))
                validationErrors.Add(loader.GetString("ValidationError_Address"));

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

        private void LoadViewModel(Guid guid)
        {
            var model = App.DriverController.Connections.FirstOrDefault(disk => disk.Descriptor.Guid.Equals(guid));
            ViewModel = new(model?.Clone());

            if (model == null && guid != Guid.Empty)
            {
                var loader = ResourceLoader.GetForViewIndependentUse();

                MainWindow.Instance!.ShowNotification(new()
                {
                    Severity = InfoBarSeverity.Error,
                    Duration = TimeSpan.FromSeconds(10),
                    Title = loader.GetString("DiskNotFound_Title"),
                    Message = string.Format(loader.GetString("DiskNotFound_Message"), guid)
                });
            }
        }

        private async Task<bool> RemindUnsavedChanges_IsExitOk()
        {
            if (!ViewModel.HasChanges) return true;
            return await SimpleMessageDialogs.UnsavedChanges(XamlRoot);
        }
    }
}
