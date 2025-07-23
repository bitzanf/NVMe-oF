using Microsoft.UI.Xaml.Data;
using System;
using Windows.ApplicationModel.Resources;
using ConnectionStatus = KernelInterface.ConnectionStatus;
using FontIcon = Microsoft.UI.Xaml.Controls.FontIcon;

namespace ManagementApp.Converters;

internal partial class ConnectionStatusToIconConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ConnectionStatus status) return null;

        return status == ConnectionStatus.Connected
            ? new FontIcon { Glyph = "\xE8CE" }
            : new FontIcon { Glyph = "\xE8CD" };
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language) => null;
}

// partial because WinRT
internal partial class ConnectionStatusToTextConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ConnectionStatus) return null;

        var loader = ResourceLoader.GetForViewIndependentUse();
        return loader.GetString($"ConnectionStatus_{value}");
    }

    public object? ConvertBack(object value, Type targetType, object parameter, string language) => null;
}