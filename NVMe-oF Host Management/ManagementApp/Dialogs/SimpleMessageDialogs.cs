using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace ManagementApp.Dialogs;

/// <summary>
/// Wrapper class to display simple warning and information dialogs
/// </summary>
public static class SimpleMessageDialogs
{
    /// <summary>
    /// You have unsaved changes, discard?
    /// </summary>
    /// <param name="xamlRoot">The current view's XAML root</param>
    /// <returns>true on confirmation</returns>
    public static async Task<bool> UnsavedChanges(XamlRoot xamlRoot)
    {
        var loader = ResourceLoader.GetForViewIndependentUse();

        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = loader.GetString("ConfirmDiscardChanges_Title"),
            PrimaryButtonText = loader.GetString("ConfirmDiscardChanges_Confirm"),
            CloseButtonText = loader.GetString("ConfirmDiscardChanges_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            Content = loader.GetString("ConfirmDiscardChanges_Text")
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// The driver's service is installed but not running, go to settings?
    /// </summary>
    /// <param name="xamlRoot">The current view's XAML root</param>
    /// <returns>true on confirmation</returns>
    public static async Task<bool> ServiceNotRunning(XamlRoot xamlRoot)
    {
        var loader = ResourceLoader.GetForViewIndependentUse();

        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = loader.GetString("ServiceNotRunningDialog_Title"),
            PrimaryButtonText = loader.GetString("ServiceNotRunningDialog_Confirm"),
            CloseButtonText = loader.GetString("ConfirmDelete_Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            Content = loader.GetString("ServiceNotRunningDialog_Text")
        };

        return await dialog.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// The driver is not installed and the application cannot be used.
    /// </summary>
    /// <param name="xamlRoot">The current view's XAML root</param>
    /// <returns></returns>
    public static async Task DriverNotInstalled(XamlRoot xamlRoot)
    {
        var loader = ResourceLoader.GetForViewIndependentUse();

        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = loader.GetString("NoDriverInstalled_Title"),
            Content = loader.GetString("NoDriverInstalled_Content"),
            CloseButtonText = loader.GetString("NoDriverInstalled_Close")
        };

        await dialog.ShowAsync();
    }
}