using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace ManagementApp.Converters;

internal class StringToUpperConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) => value?.ToString()?.ToUpper() ?? string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => null;
}