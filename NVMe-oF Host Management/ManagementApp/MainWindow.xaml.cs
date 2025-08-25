using CommunityToolkit.WinUI.Behaviors;
using ManagementApp.Converters;
using ManagementApp.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using ManagementApp.Dialogs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp;

/// <summary>
/// Main application window hosting the navigation menu, notification area and content frame
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Cache for selected navigation items
    /// </summary>
    private readonly Dictionary<string, NavigationViewItem> _navigationTagSelected;

    /// <summary>
    /// Cache for navigation items to navigate to
    /// </summary>
    private readonly Dictionary<string, Type> _navigationTagTargets;

    public delegate Task<bool> NavigationRequested();
    public event NavigationRequested? OnNavigationRequested;

    /// <summary>
    /// Main Window singleton
    /// </summary>
    public static MainWindow? Instance { get; private set; }

    /// <summary>
    /// Header text to be displayed by <see cref="Controls.AutoPageHeader"/>
    /// </summary>
    public string? PageHeader => ((NavigationViewItem)NavigationViewControl.SelectedItem)?.Content.ToString();

    public MainWindow()
    {
        // Initialize the window and set a custom title bar
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };

        // Set custom application titlebar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Set minimum window size
        var presenter = OverlappedPresenter.Create();
        // Reasonable values for a very small yet usable window
        presenter.PreferredMinimumWidth = 680;
        presenter.PreferredMinimumHeight = 340;

        AppWindow.SetPresenter(presenter);

        // Initialize navigation cache for the NavigationView
        var navItems = NavigationViewControl.MenuItems
            .Concat(NavigationViewControl.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .ToList();

        _navigationTagTargets = navItems
            .Select(nav => nav.Tag.ToString()!)
            .ToDictionary(tag => tag, tag => Type.GetType(tag)!);

        _navigationTagSelected = navItems
            .ToDictionary(nav => nav.Tag.ToString()!);
        
        // Navigate to first page in the list
        var firstItem = navItems.First();
        NavigationViewControl.SelectedItem = firstItem;
        NavigateToTag(firstItem.Tag.ToString()!, new EntranceNavigationTransitionInfo());

        Instance = this;

        ContentFrame.Loaded += async (_, _) => await ExceptionToNotificationConverter.WrapExceptionsAsync(InitializeConnection);
    }

    /// <summary>
    /// Show the notification in the window's notification area
    /// </summary>
    /// <param name="notification"></param>
    /// <returns></returns>
    public Notification ShowNotification(Notification notification) => NotificationQueue.Show(notification);

    /// <summary>
    /// Check that the driver is installed (if not, inform the user and exit the app), check that the driver service is running (if not, direct the user to Settings),
    /// connect to the driver and load all connections for the first time
    /// </summary>
    /// <returns></returns>
    private async Task InitializeConnection()
    {
        if (App.DriverController.IsConnected) return;
        var loader = ResourceLoader.GetForViewIndependentUse();

        Helpers.DriverServiceHelper service = new();
        if (!service.IsPresent)
        {
            // The driver can't be found, so there's no point in using the app
            await SimpleMessageDialogs.DriverNotInstalled(ContentFrame.XamlRoot);
            Application.Current.Exit();
        }
        else if (!service.IsRunning)
        {
            if (await SimpleMessageDialogs.ServiceNotRunning(ContentFrame.XamlRoot))
                ContentFrame.Navigate(typeof(SettingsPage));
            // No point in trying to load connection, since the driver is not running...
            else return;
        }

        try
        {
            App.DriverController.ConnectToDriver();
        }
        catch (Exception ex)
        {
            var notification = new Notification
            {
                Title = loader.GetString("DriverConnectionError_Title"),
                Message = ex.Message,
                Severity = InfoBarSeverity.Error
            };
            ShowNotification(notification);
            return;
        }
        
        await App.DriverController.LoadConnections();
    }

    /// <summary>
    /// Change the selected navigation item to the currently displayed page
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;
        SelectNavItemByType(ContentFrame.SourcePageType);
    }

    /// <summary>
    /// If we need approval for exiting the current page (such as unsaved changes), perform the appropriate action; otherwise simply go back a page
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void NavigationViewControl_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () =>
        {
            if (ContentFrame.CanGoBack)
            {
                var approved = await (OnNavigationRequested?.Invoke() ?? Task.FromResult(true));
                if (approved) ContentFrame.GoBack();
            }
        });

    /// <summary>
    /// Navigate the <see cref="ContentFrame"/> to the selected item in the <see cref="NavigationViewControl"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void NavigationViewControl_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        => await ExceptionToNotificationConverter.WrapExceptionsAsync(async () => {
            var item = NavigationViewControl.SelectedItem as NavigationViewItem;
            var invokedTag = args.InvokedItemContainer?.Tag?.ToString();

            if (item != null)
            {
                var currentPageType = ContentFrame.CurrentSourcePageType;
                if (currentPageType.FullName == invokedTag) return;

                var approved = await (OnNavigationRequested?.Invoke() ?? Task.FromResult(true));
                if (!approved)
                {
                    SelectNavItemByType(currentPageType);
                    return;
                }
            }

            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            }
            else if (invokedTag != null)
            {
                NavigateToTag(invokedTag, args.RecommendedNavigationTransitionInfo);
            }
        });

    /// <summary>
    /// Navigate the <see cref="ContentFrame"/> to the page given by the tag in the <see cref="_navigationTagTargets"/>
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="navigationTransitionInfo"></param>
    private void NavigateToTag(string tag, NavigationTransitionInfo? navigationTransitionInfo)
        => ContentFrame.Navigate(_navigationTagTargets[tag], null, navigationTransitionInfo);

    /// <summary>
    /// Select the appropriate selected item in the <see cref="NavigationViewControl"/>
    /// <br /> <br />
    /// SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag
    /// We may be navigating to a page that is not in the sidebar (i.e. DiskEditPage), in that case show no selected item
    /// </summary>
    /// <param name="type"></param>
    private void SelectNavItemByType(Type type)
        => NavigationViewControl.SelectedItem =
            type == typeof(SettingsPage)
                ? NavigationViewControl.SettingsItem as NavigationViewItem
                : _navigationTagSelected.GetValueOrDefault(type.FullName ?? string.Empty);

#pragma warning disable CA1822
    // It can't be static because it's used in XAML bindings
    private string GetAppTitleFromSystem() => AppInfo.Current.DisplayInfo.DisplayName;
#pragma warning restore CA1822
}