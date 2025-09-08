using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using KernelInterface;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ManagementApp.Dialogs;

/// <summary>
/// Dialog that presents the user with details of a connection they are about to delete (as a modal window)
/// </summary>
internal sealed partial class ConfirmDeleteDialog : Page
{
    /// <summary>
    /// The connection about to be deleted
    /// </summary>
    private readonly Models.DiskConnectionModel _connection;

    /// <summary>
    /// The connection's network information
    /// </summary>
    private NetworkConnection NetworkConnection => _connection.Descriptor.NetworkConnection;

    /// <summary>
    /// Textual representation of the network connection address' type for display
    /// </summary>
    public string AddressTypeString => $"{NetworkConnection.AddressFamily} / {NetworkConnection.TransportType.ToString().ToUpper()}";

    /// <summary>
    /// Textual representation of the network connection address and port for display
    /// </summary>
    public string AddressString => $"{NetworkConnection.TransportAddress} : {NetworkConnection.TransportServiceId}";


    private ConfirmDeleteDialog(Models.DiskConnectionModel connection)
    {
        InitializeComponent();
        _connection = connection;
    }

    /// <summary>
    /// Displays the warning dialog and asks for confirmation
    /// </summary>
    /// <param name="xamlRoot">The current view's XAML root</param>
    /// <param name="connection">The connection requested to be deleted by the user</param>
    /// <returns>ContentDialogResult.Primary on delete confirmation</returns>
    internal static async Task<ContentDialogResult> ShowDialog(XamlRoot xamlRoot, Models.DiskConnectionModel connection)
    {
        var loader = ResourceLoader.GetForViewIndependentUse();

        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            Style = Application.Current.Resources["DefaultContentDialogStyle"] as Style,
            Title = loader.GetString("ConfirmDelete_Title"),
            PrimaryButtonText = loader.GetString("ConfirmDelete_Confirm"),
            CloseButtonText = loader.GetString("ConfirmDelete_Cancel"),
            DefaultButton = ContentDialogButton.Close,
            Content = new ConfirmDeleteDialog(connection)
        };

        return await dialog.ShowAsync();
    }
}
