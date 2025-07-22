using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using CommunityToolkit.WinUI.Behaviors;
using ManagementApp.Views;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Media.Animation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly Dictionary<string, NavigationViewItem> _navigationTagSelected;
    private readonly Dictionary<string, Type> _navigationTagTargets;

    public delegate Task<bool> NavigationRequested();
    public event NavigationRequested? OnNavigationRequested;

    public static MainWindow? Instance { get; private set; }

    public string? PageHeader => ((NavigationViewItem)NavigationViewControl.SelectedItem)?.Content.ToString();

    public MainWindow()
    {
        // Initialize the window and set a custom title bar
        InitializeComponent();

        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

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

        Activated += OnActivated;
    }

    public Notification ShowNotification(string notification, int msDuration = 0, string? title = null) =>
        NotificationQueue.Show(notification, msDuration, title);

    public Notification ShowNotification(Notification notification) => NotificationQueue.Show(notification);

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        // The Activated event gets raised when focusing the window as well
        if (App.DriverController.IsConnected) return;

        // TODO: Check kernel connection, possibly ask for service install

        // ...
        try
        {
            App.DriverController.ConnectToDriver(@"\\.\NvmeOfController");
        }
        catch (Exception ex)
        {
            // TODO: Handle, probably show error dialog...
        }

        // TODO: Remove ugly hack
        App.DriverController.LoadConnections();
    }

    private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;

        if (ContentFrame.SourcePageType == typeof(SettingsPage))
        {
            // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
            NavigationViewControl.SelectedItem = NavigationViewControl.SettingsItem;
        } else if (ContentFrame.SourcePageType != null)
        {
            // We may be navigating to a page that is not in the sidebar (i.e. DiskEditPage), in that case show no selected item
            NavigationViewControl.SelectedItem = _navigationTagSelected.GetValueOrDefault(ContentFrame.SourcePageType.FullName!);
        }
    }

    private void NavigationViewControl_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack) ContentFrame.GoBack();
    }

    private async void NavigationViewControl_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        var item = NavigationViewControl.SelectedItem as NavigationViewItem;
        var invokedTag = args.InvokedItemContainer?.Tag?.ToString();

        if (item != null)
        {
            var currentPageType = ContentFrame.CurrentSourcePageType;
            if (currentPageType.FullName == invokedTag) return;

            var approved = await (OnNavigationRequested?.Invoke() ?? Task.FromResult(true));
            if (!approved)
            {
                NavigationViewControl.SelectedItem = currentPageType == typeof(SettingsPage)
                    ? NavigationViewControl.SettingsItem
                    : _navigationTagSelected[currentPageType.FullName!];
                
                return;
            }
        }

        if (args.IsSettingsInvoked)
        {
            ContentFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);
        } else if (invokedTag != null)
        {
            NavigateToTag(invokedTag, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavigateToTag(string tag, NavigationTransitionInfo? navigationTransitionInfo)
        => ContentFrame.Navigate(_navigationTagTargets[tag], null, navigationTransitionInfo);

    // It can't be static because it's used in XAML bindings
#pragma warning disable CA1822
    private string GetAppTitleFromSystem() => AppInfo.Current.DisplayInfo.DisplayName;
#pragma warning restore CA1822
}