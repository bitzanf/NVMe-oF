using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ManagementApp.Controls;

/// <summary>
/// UserControl representing a universal page header <br />
/// The header is automatically acquired from the selected NavigationItem in the main window,
/// however it can also be explicitly set by the user
/// </summary>
public sealed partial class AutoPageHeader : UserControl
{
    /// <summary>
    /// Actual header text to be displayed by the control
    /// </summary>
    private string Header => CustomHeader ?? MainWindow.Instance?.PageHeader ?? "<null>";

    private static readonly DependencyProperty CustomHeaderDependencyProperty = DependencyProperty.Register(
        nameof(CustomHeader),
        typeof(string),
        typeof(AutoPageHeader),
        null
    );

    /// <summary>
    /// User-defined header text to overwrite the automatic one
    /// </summary>
    public string? CustomHeader
    {
        get => (string)GetValue(CustomHeaderDependencyProperty);
        set => SetValue(CustomHeaderDependencyProperty, value);
    }

    public AutoPageHeader() => InitializeComponent();
}
