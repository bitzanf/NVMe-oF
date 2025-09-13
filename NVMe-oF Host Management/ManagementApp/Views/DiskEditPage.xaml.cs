using CommunityToolkit.WinUI.Behaviors;
using ManagementApp.Helpers;
using ManagementApp.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using ManagementApp.Converters;
using ManagementApp.Dialogs;

namespace ManagementApp.Views
{
    /// <summary>
    /// Page that allows the user to edit a single connection (disk)
    /// </summary>
    public sealed partial class DiskEditPage : Page
    {
        internal DiskEditViewModel ViewModel = null!;

        /// <summary>
        /// Show the disk's Guid and object path?
        /// </summary>
        public bool ShowInfo => !string.IsNullOrEmpty(ViewModel?.NtObjectPath);

        public DiskEditPage()
        {
            InitializeComponent();

            Loaded += (_, _) => MainWindow.Instance!.OnNavigationRequested += RemindUnsavedChanges_IsExitOk;
            Unloaded += (_, _) => MainWindow.Instance!.OnNavigationRequested -= RemindUnsavedChanges_IsExitOk;
        }

        // https://learn.microsoft.com/en-us/windows/apps/design/basics/navigate-between-two-pages
        /// <summary>
        /// Load an existing connection or create a new one, depending on if we got a valid Guid
        /// </summary>
        /// <param name="e"></param>
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

        /// <summary>
        /// Validate &amp; save the current changes and exit the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSave_OnClick(object sender, RoutedEventArgs e)
            => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
            {
                if (!Validate()) return;

                bool success;
                if (ViewModel.Model.Descriptor.Guid == Guid.Empty)
                    success = await App.DriverController.AddNewConnection(ViewModel.Model);
                else
                    success = await App.DriverController.IntegrateChanges(ViewModel.Model);

                NotificationHelper.ShowChangeSaveStatus(success);

                // We've edited and saved the disk, return to previous page (likely DiskSetupPage)
                Frame.GoBack();
            });

        /// <summary>
        /// Cancel the current changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_OnClick(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }

        /// <summary>
        /// Simple validation of the given data; validation errors get shown as a notification
        /// </summary>
        /// <returns></returns>
        private bool Validate()
        {
            var loader = ResourceLoader.GetForViewIndependentUse();

            var descriptor = ViewModel.Model.Descriptor;
            List<string> validationErrors = [];

            if (string.IsNullOrWhiteSpace(descriptor.Nqn))
                validationErrors.Add(loader.GetString("ValidationError_Nqn"));
            
            if (string.IsNullOrWhiteSpace(descriptor.NetworkConnection.TransportAddress))
                validationErrors.Add(loader.GetString("ValidationError_Address"));

            if (descriptor.NetworkConnection.TransportServiceId == 0)
                validationErrors.Add(loader.GetString("ValidationError_Port"));

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

        /// <summary>
        /// Load connection information by the given guid
        /// </summary>
        /// <param name="guid"></param>
        private void LoadViewModel(Guid guid)
        {
            var model = App.DriverController.TryGetModel(guid);
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
}
