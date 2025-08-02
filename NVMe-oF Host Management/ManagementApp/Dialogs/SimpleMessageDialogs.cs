using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace ManagementApp.Dialogs;

public static class SimpleMessageDialogs
{
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