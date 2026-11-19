namespace LaraLauncher.Core.Services;

public readonly record struct WeatherConditionInfo(string Description, string Icon);

/// <summary>
/// Maps WMO weather interpretation codes (used by Open-Meteo) to Spanish descriptions and icons.
/// </summary>
public static class WmoWeatherMapper
{
    public static WeatherConditionInfo Map(int wmoCode) => wmoCode switch
    {
        0 => new("Despejado", "☀"),
        1 => new("Mayormente despejado", "🌤"),
        2 => new("Parcialmente nublado", "⛅"),
        3 => new("Nublado", "☁"),
        45 or 48 => new("Niebla", "🌫"),
        51 or 53 or 55 => new("Llovizna", "🌦"),
        56 or 57 => new("Llovizna helada", "🌧"),
        61 or 63 => new("Lluvia", "🌧"),
        65 => new("Lluvia intensa", "🌧"),
        66 or 67 => new("Lluvia helada", "🌧"),
        71 or 73 or 75 => new("Nieve", "❄"),
        77 => new("Granizo fino", "❄"),
        80 or 81 or 82 => new("Chubascos", "🌦"),
        85 or 86 => new("Chubascos de nieve", "❄"),
        95 => new("Tormenta eléctrica", "⛈"),
        96 or 99 => new("Tormenta con granizo", "⛈"),
        _ => new("Variable", "⛅")
    };
}
