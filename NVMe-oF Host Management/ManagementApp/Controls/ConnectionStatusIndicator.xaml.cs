using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

using ConnectionStatusEnum = ManagementApp.Models.DiskConnectionModel.ConnectionStatusEnum;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ManagementApp.Controls;

// https://stackoverflow.com/questions/72350934/how-to-implement-a-winui-3-usercontrol-with-combobox-enum-and-dependencypropert
// https://stackoverflow.com/questions/40184194/xaml-binding-not-working-on-dependency-property
internal sealed partial class ConnectionStatusIndicator : UserControl
{
    public ConnectionStatusEnum ConnectionStatus
    {
        get => (ConnectionStatusEnum)GetValue(ConnectionStatusDependencyProperty);
        set
        {
            SetValue(ConnectionStatusDependencyProperty, value);
            GoToState(value);
        }
    }

    public static readonly DependencyProperty ConnectionStatusDependencyProperty = DependencyProperty.Register(
        nameof(ConnectionStatus),
        typeof(ConnectionStatusEnum),
        typeof(ConnectionStatusIndicator),
        new PropertyMetadata(ConnectionStatusEnum.Disconnected)
    );

    public ConnectionStatusIndicator()
    {
        InitializeComponent();

        Loaded += (_, _) => GoToState(ConnectionStatus);
    }

    private void GoToState(ConnectionStatusEnum status)
    {
        var state = status switch
        {
            ConnectionStatusEnum.Disconnected => "Disconnected",
            ConnectionStatusEnum.Connecting => "Connecting",
            ConnectionStatusEnum.Connected => "Connected",

            _ => throw new ArgumentException(status.ToString(), nameof(status))
        };

        VisualStateManager.GoToState(this, state, false);
    }
}