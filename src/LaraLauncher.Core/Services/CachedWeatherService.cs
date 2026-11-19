using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Services;

/// <summary>
/// Decorator over IWeatherService providing TTL caching and graceful offline fallback (OCP/SRP).
/// </summary>
public sealed class CachedWeatherService : IWeatherService
{
    private readonly IWeatherService _inner;
    private readonly IWeatherCacheStore _cacheStore;
    private readonly TimeSpan _cacheTtl;
    private readonly Func<DateTimeOffset> _utcNow;
    private WeatherForecast? _memoryCache;

    public CachedWeatherService(
        IWeatherService inner,
        IWeatherCacheStore cacheStore,
        TimeSpan? cacheTtl = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _inner = inner;
        _cacheStore = cacheStore;
        _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(30);
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public async Task<WeatherForecast?> GetForecastAsync(
        GeoLocation location,
        int days = 7,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        var cached = await GetMatchingCacheAsync(location, cancellationToken).ConfigureAwait(false);
        DateTimeOffset now = _utcNow();

        if (!forceRefresh && cached is not null && (now - cached.RetrievedAtUtc) <= _cacheTtl)
        {
            cached.IsFromCache = true;
            return cached;
        }

        try
        {
            var fresh = await _inner.GetForecastAsync(location, days, forceRefresh, cancellationToken).ConfigureAwait(false);
            if (fresh is not null)
            {
                fresh.IsFromCache = false;
                _memoryCache = fresh;
                await _cacheStore.WriteAsync(fresh, cancellationToken).ConfigureAwait(false);
                return fresh;
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Fall through to cached snapshot when offline or remote endpoint fails.
        }

        if (cached is not null)
        {
            cached.IsFromCache = true;
            return cached;
        }

        return null;
    }

    private async Task<WeatherForecast?> GetMatchingCacheAsync(GeoLocation location, CancellationToken cancellationToken)
    {
        if (_memoryCache is not null && IsSameLocation(_memoryCache, location))
        {
            return _memoryCache;
        }

        var diskCache = await _cacheStore.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (diskCache is not null && IsSameLocation(diskCache, location))
        {
            _memoryCache = diskCache;
            return diskCache;
        }

        return null;
    }

    private static bool IsSameLocation(WeatherForecast forecast, GeoLocation location)
    {
        return Math.Abs(forecast.Latitude - location.Latitude) < 0.05 &&
               Math.Abs(forecast.Longitude - location.Longitude) < 0.05;
    }
}
