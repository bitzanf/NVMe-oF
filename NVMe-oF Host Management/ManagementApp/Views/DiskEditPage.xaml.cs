using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DiskEditPage : Page
    {
        internal ViewModels.DiskEditViewModel ViewModel = null!;

        public DiskEditPage()
        {
            InitializeComponent();
        }

        // https://learn.microsoft.com/en-us/windows/apps/design/basics/navigate-between-two-pages
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter == null)
            {
                // TODO: We are requested to create a new connection
                //  some details may be present in App.DriverController.NewlyConfiguredModel (quick connect from discovery), check them
            }

            if (e.Parameter is Guid guid)
            {
                // TODO: Get disk model
                UuidRun.Text = guid.ToString();
            }

            ViewModel = new();

            base.OnNavigatedTo(e);
        }
    }
}
