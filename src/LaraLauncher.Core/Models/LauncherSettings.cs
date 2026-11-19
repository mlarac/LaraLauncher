namespace LaraLauncher.Core.Models;

public enum TemperatureUnit
{
    Celsius,
    Fahrenheit
}

/// <summary>
/// User preferences persisted automatically in %AppData%\LaraLauncher\config.json.
/// </summary>
public sealed class LauncherSettings
{
    public bool StartWithWindows { get; set; }
    public bool StartFullscreen { get; set; } = true;
    public bool EnableGamepad { get; set; } = true;
    public bool EnableNavigationSounds { get; set; } = true;
    public int GridColumns { get; set; } = 3;
    public string AccentHex { get; set; } = "#7E4AE8";

    // Weather settings (consumed in Phase 4)
    public string WeatherCityName { get; set; } = string.Empty;
    public double? WeatherLatitude { get; set; }
    public double? WeatherLongitude { get; set; }
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Celsius;
}
