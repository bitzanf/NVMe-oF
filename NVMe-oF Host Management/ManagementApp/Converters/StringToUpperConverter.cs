using System;
using Microsoft.UI.Xaml.Data;

namespace ManagementApp.Converters;

internal partial class StringToUpperConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
        => value?.ToString()?.ToUpper();

    public object? ConvertBack(object value, Type targetType, object parameter, string language) => null;
}