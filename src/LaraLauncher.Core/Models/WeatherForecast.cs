namespace LaraLauncher.Core.Models;

public sealed class CurrentWeather
{
    public double TemperatureC { get; set; }
    public double ApparentTemperatureC { get; set; }
    public int HumidityPercent { get; set; }
    public double WindSpeedKmh { get; set; }
    public int WeatherCode { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIcon { get; set; } = "⛅";
}

public sealed class DailyForecast
{
    public DateOnly Date { get; set; }
    public double MaxTemperatureC { get; set; }
    public double MinTemperatureC { get; set; }
    public int PrecipitationProbabilityPercent { get; set; }
    public int WeatherCode { get; set; }
    public string ConditionText { get; set; } = string.Empty;
    public string ConditionIcon { get; set; } = "⛅";
}

public sealed class WeatherForecast
{
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsFromCache { get; set; }
    public CurrentWeather Current { get; set; } = new();
    public List<DailyForecast> DailyDays { get; set; } = new();
}
