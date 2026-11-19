using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Persistence abstraction for cached weather snapshots (DIP/ISP).
/// </summary>
public interface IWeatherCacheStore
{
    Task<WeatherForecast?> ReadAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(WeatherForecast forecast, CancellationToken cancellationToken = default);
}
