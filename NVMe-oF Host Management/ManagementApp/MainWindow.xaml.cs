using KernelInterface;
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
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
        var firstItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().First();
        NavigationViewControl.SelectedItem = firstItem;

        NavigateToTag(firstItem.Tag.ToString()!, new EntranceNavigationTransitionInfo());
    }

    private void ContentFrame_OnNavigated(object sender, NavigationEventArgs e)
    {
        NavigationViewControl.IsBackEnabled = ContentFrame.CanGoBack;

        if (ContentFrame.SourcePageType == typeof(Pages.SettingsPage))
        {
            // SettingsItem is not part of NavView.MenuItems, and doesn't have a Tag.
            NavigationViewControl.SelectedItem = NavigationViewControl.SettingsItem;
        } else if (ContentFrame.SourcePageType != null)
        {
            NavigationViewControl.SelectedItem = _navigationTagSelected[ContentFrame.SourcePageType.FullName!];
        }

        NavigationViewControl.Header = ((NavigationViewItem)NavigationViewControl.SelectedItem)?.Content.ToString();
    }

    private void MainNavigation_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack) ContentFrame.GoBack();
    }

    private void MainNavigation_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            ContentFrame.Navigate(typeof(Pages.SettingsPage), null, args.RecommendedNavigationTransitionInfo);
        } else if (args.InvokedItemContainer != null && (args.InvokedItemContainer.Tag != null))
        {
            var tag = args.InvokedItemContainer.Tag.ToString()!;
            NavigateToTag(tag, args.RecommendedNavigationTransitionInfo);
        }
    }

    private void NavigateToTag(string tag, NavigationTransitionInfo? navigationTransitionInfo)
        => ContentFrame.Navigate(_navigationTagTargets[tag], null, navigationTransitionInfo);

    private string GetAppTitleFromSystem() => AppInfo.Current.DisplayInfo.DisplayName;
}