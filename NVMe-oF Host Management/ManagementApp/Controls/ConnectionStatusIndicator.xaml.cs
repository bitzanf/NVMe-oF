using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

using ConnectionStatus = KernelInterface.ConnectionStatus;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Controls;

// https://stackoverflow.com/questions/72350934/how-to-implement-a-winui-3-usercontrol-with-combobox-enum-and-dependencypropert
// https://stackoverflow.com/questions/40184194/xaml-binding-not-working-on-dependency-property
internal sealed partial class ConnectionStatusIndicator : UserControl
{
    public ConnectionStatus ConnectionStatus
    {
        get => (ConnectionStatus)GetValue(ConnectionStatusDependencyProperty);
        set
        {
            SetValue(ConnectionStatusDependencyProperty, value);
            GoToState(value);
        }
    }

    public double Radius
    {
        get => (double)GetValue(RadiusDependencyProperty);
        set => SetValue(RadiusDependencyProperty, value);
    }

    public static readonly DependencyProperty ConnectionStatusDependencyProperty = DependencyProperty.Register(
        nameof(ConnectionStatus),
        typeof(ConnectionStatus),
        typeof(ConnectionStatusIndicator),
        new PropertyMetadata(ConnectionStatus.Disconnected)
    );

    public static readonly DependencyProperty RadiusDependencyProperty = DependencyProperty.Register(
        nameof(Radius),
        typeof(double),
        typeof(ConnectionStatusIndicator),
        new PropertyMetadata(2.0)
    );

    public ConnectionStatusIndicator()
    {
        InitializeComponent();

        Loaded += (_, _) => GoToState(ConnectionStatus);
    }

    private void GoToState(ConnectionStatus status)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException(status.ToString(), nameof(status));
        VisualStateManager.GoToState(this, status.ToString(), false);
    }
}