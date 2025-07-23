using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using KernelInterface;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class ConfirmDeleteDialog : Page
{
    private readonly Models.DiskConnectionModel _connection;

    private NetworkConnection NetworkConnection => _connection.Descriptor.NetworkConnection;

    public string AddressTypeString => $"{NetworkConnection.AddressFamily} / {NetworkConnection.TransportType}";
    public string AddressString => $"{NetworkConnection.TransportAddress} : {NetworkConnection.TransportServiceId}";


    private ConfirmDeleteDialog(Models.DiskConnectionModel connection)
    {
        InitializeComponent();
        _connection = connection;
    }

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
