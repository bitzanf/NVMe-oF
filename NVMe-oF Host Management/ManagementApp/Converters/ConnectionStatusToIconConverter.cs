using Microsoft.UI.Xaml.Data;
using System;

using ConnectionStatusEnum = ManagementApp.Models.DiskConnectionModel.ConnectionStatusEnum;
using FontIcon = Microsoft.UI.Xaml.Controls.FontIcon;

namespace ManagementApp.Converters;

internal class ConnectionStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not ConnectionStatusEnum status) return null;

        return status == ConnectionStatusEnum.Connected
            ? new FontIcon { Glyph = "\xE8CE" }
            : new FontIcon { Glyph = "\xE8CD" };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
}