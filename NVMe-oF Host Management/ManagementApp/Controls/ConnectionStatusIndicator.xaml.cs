using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

using ConnectionStatus = KernelInterface.ConnectionStatus;

namespace ManagementApp.Controls;

// https://stackoverflow.com/questions/72350934/how-to-implement-a-winui-3-usercontrol-with-combobox-enum-and-dependencypropert
// https://stackoverflow.com/questions/40184194/xaml-binding-not-working-on-dependency-property
/// <summary>
/// An animated indicator displaying the connection's current status
/// </summary>
internal sealed partial class ConnectionStatusIndicator : UserControl
{

    /// <summary>
    /// The connection's current status
    /// </summary>
    public ConnectionStatus ConnectionStatus
    {
        get => (ConnectionStatus)GetValue(ConnectionStatusDependencyProperty);
        set
        {
            SetValue(ConnectionStatusDependencyProperty, value);
            GoToState(value);
        }
    }

    /// <summary>
    /// Corner radius of the indicator
    /// </summary>
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

    /// <summary>
    /// Helper function to change animation states on connection status change
    /// </summary>
    /// <param name="status">New status to change the animation into</param>
    /// <exception cref="ArgumentException"></exception>
    private void GoToState(ConnectionStatus status)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentException(status.ToString(), nameof(status));
        VisualStateManager.GoToState(this, status.ToString(), false);
    }
}