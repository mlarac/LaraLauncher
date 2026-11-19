using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

public interface IWeatherService
{
    Task<WeatherForecast?> GetForecastAsync(
        GeoLocation location,
        int days = 7,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default);
}
