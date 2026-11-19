using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LaraLauncher.App.Converters;

public sealed class HexToBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush FallbackBrush = new(Color.FromRgb(0x3B, 0x26, 0x67));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                return brush;
            }
            catch (FormatException)
            {
                // Fallback to default Roku surface accent.
            }
        }

        return FallbackBrush;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
