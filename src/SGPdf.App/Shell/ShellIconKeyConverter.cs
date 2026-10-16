using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SGPdf.App.Shell;

public sealed class ShellIconKeyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is string key && Application.Current?.TryFindResource(key) is Geometry geometry
            ? geometry
            : DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
