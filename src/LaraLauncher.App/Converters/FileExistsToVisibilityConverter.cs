using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace LaraLauncher.App.Converters;

/// <summary>
/// Returns Visible when a file path exists on disk; pass ConverterParameter="Invert" to invert.
/// </summary>
public sealed class FileExistsToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool exists = value is string path && !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
        bool visible = invert ? !exists : exists;
        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
