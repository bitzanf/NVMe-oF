using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ManagementApp.Converters;
using ManagementApp.ViewModels;

namespace ManagementApp.Views;

/// <summary>
/// View displaying various driver statistics to the user
/// </summary>
public sealed partial class StatsPage : Page
{
    internal StatsViewModel ViewModel = new();

    public bool IsConnected => App.DriverController.IsConnected;

    public StatsPage()
    {
        InitializeComponent();
        Reload();
    }

    private void BtnRefresh_OnClick(object sender, RoutedEventArgs e) => Reload();

    private void Reload() => ExceptionToNotificationConverter.WrapExceptions(ViewModel.Reload);

#pragma warning disable CA1822
    /// <summary>
    /// Automatically scales the given value to Giga/Mega/Kilo
    /// </summary>
    /// <param name="value"></param>
    /// <param name="base2">true for base2, false for base10</param>
    /// <returns></returns>
    internal string AutoScale(ulong value, bool base2)
    {
        double scaling = base2 ? 1024 : 1000;
        
        var kb = scaling;
        var mb = scaling * kb;
        var gb = scaling * mb;

        double div = value;
        char? scale = null;
        if (value > gb)
        {
            div = value / gb;
            scale = 'G';
        } else if (value > mb)
        {
            div = value / mb;
            scale = 'M';
        } else if (value > kb)
        {
            div = value / kb;
            scale = 'K';
        }

        return $"{div:F3} {scale}{(scale.HasValue && base2 ? "i" : string.Empty)}B";
    }
#pragma warning restore CA1822
}