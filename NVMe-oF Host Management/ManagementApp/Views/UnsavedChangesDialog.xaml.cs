using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class UnsavedChangesDialog : Page
{
    private UnsavedChangesDialog() => InitializeComponent();

    internal static async Task<ContentDialogResult> ShowDialog(XamlRoot xamlRoot)
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
            Content = new UnsavedChangesDialog()
        };

        return await dialog.ShowAsync();
    }
}
