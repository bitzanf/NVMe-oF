using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ManagementApp.Controls;

public sealed partial class AutoPageHeader : UserControl
{
    private string Header => CustomHeader ?? MainWindow.Instance?.PageHeader ?? "<null>";

    private static readonly DependencyProperty CustomHeaderDependencyProperty = DependencyProperty.Register(
        nameof(CustomHeader),
        typeof(string),
        typeof(AutoPageHeader),
        null
    );

    public string? CustomHeader
    {
        get => (string)GetValue(CustomHeaderDependencyProperty);
        set => SetValue(CustomHeaderDependencyProperty, value);
    }

    public AutoPageHeader() => InitializeComponent();

}
