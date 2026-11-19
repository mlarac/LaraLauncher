using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Services;

/// <summary>
/// Converts and formats Celsius temperatures into the user's preferred unit.
/// </summary>
public static class TemperatureFormatter
{
    public static double Convert(double celsius, TemperatureUnit unit) =>
        unit == TemperatureUnit.Fahrenheit
            ? (celsius * 9.0 / 5.0) + 32.0
            : celsius;

    public static string Format(double celsius, TemperatureUnit unit)
    {
        int rounded = (int)Math.Round(Convert(celsius, unit), MidpointRounding.AwayFromZero);
        string suffix = unit == TemperatureUnit.Fahrenheit ? "°F" : "°C";
        return $"{rounded}{suffix}";
    }

    public static string FormatShort(double celsius, TemperatureUnit unit)
    {
        int rounded = (int)Math.Round(Convert(celsius, unit), MidpointRounding.AwayFromZero);
        return $"{rounded}°";
    }
}
