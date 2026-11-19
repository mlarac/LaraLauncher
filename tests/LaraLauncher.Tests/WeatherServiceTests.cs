using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;
using LaraLauncher.Infrastructure.Weather;

namespace LaraLauncher.Tests;

public sealed class WeatherServiceTests
{
    [Theory]
    [InlineData(0, "Despejado")]
    [InlineData(2, "Parcialmente nublado")]
    [InlineData(61, "Lluvia")]
    [InlineData(95, "Tormenta eléctrica")]
    [InlineData(999, "Variable")]
    public void WmoWeatherMapper_MapsExpectedSpanishDescriptions(int code, string expectedText)
    {
        var info = WmoWeatherMapper.Map(code);

        Assert.Equal(expectedText, info.Description);
        Assert.False(string.IsNullOrWhiteSpace(info.Icon));
    }

    [Fact]
    public void TemperatureFormatter_ConvertsAndFormatsCelsiusAndFahrenheit()
    {
        Assert.Equal("20°C", TemperatureFormatter.Format(20.2, TemperatureUnit.Celsius));
        Assert.Equal("68°F", TemperatureFormatter.Format(20.0, TemperatureUnit.Fahrenheit));
        Assert.Equal("32°F", TemperatureFormatter.Format(0.0, TemperatureUnit.Fahrenheit));
    }

    [Fact]
    public void OpenMeteoParser_ParsesForecastJsonCorrectly()
    {
        const string sampleJson = """
        {
          "current": {
            "temperature_2m": 18.6,
            "apparent_temperature": 17.9,
            "relative_humidity_2m": 64,
            "wind_speed_10m": 12.4,
            "weather_code": 2
          },
          "daily": {
            "time": ["2026-11-19", "2026-11-20", "2026-11-21"],
            "weather_code": [2, 61, 0],
            "temperature_2m_max": [21.5, 16.0, 23.8],
            "temperature_2m_min": [9.2, 8.5, 10.1],
            "precipitation_probability_max": [15, 80, 5]
          }
        }
        """;

        var loc = new GeoLocation { Name = "Temuco", Country = "Chile", Latitude = -38.7359, Longitude = -72.5904 };
        var forecast = OpenMeteoWeatherService.ParseForecastJson(sampleJson, loc);

        Assert.NotNull(forecast);
        Assert.Equal("Temuco, Chile", forecast.LocationName);
        Assert.Equal(18.6, forecast.Current.TemperatureC);
        Assert.Equal(64, forecast.Current.HumidityPercent);
        Assert.Equal("Parcialmente nublado", forecast.Current.ConditionText);
        Assert.Equal(3, forecast.DailyDays.Count);
        Assert.Equal(new DateOnly(2026, 11, 20), forecast.DailyDays[1].Date);
        Assert.Equal("Lluvia", forecast.DailyDays[1].ConditionText);
        Assert.Equal(80, forecast.DailyDays[1].PrecipitationProbabilityPercent);
    }

    [Fact]
    public void OpenMeteoParser_ParsesGeocodingJsonCorrectly()
    {
        const string sampleGeoJson = """
        {
          "results": [
            { "name": "Temuco", "admin1": "Araucanía", "country": "Chile", "latitude": -38.7359, "longitude": -72.5904 },
            { "name": "Santiago", "admin1": "Metropolitana", "country": "Chile", "latitude": -33.4569, "longitude": -70.6483 }
          ]
        }
        """;

        var results = OpenMeteoWeatherService.ParseGeocodingJson(sampleGeoJson);

        Assert.Equal(2, results.Count);
        Assert.Equal("Temuco, Araucanía, Chile", results[0].DisplayName);
        Assert.Equal(-38.7359, results[0].Latitude);
    }

    [Fact]
    public async Task CachedWeatherService_UsesCacheWithinTtl_AndFallsBackOfflineWhenExpired()
    {
        var now = new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.Zero);
        var loc = new GeoLocation { Name = "Temuco", Latitude = -38.73, Longitude = -72.59 };
        var store = new InMemoryWeatherCacheStore();
        var inner = new FakeWeatherService(() => new WeatherForecast
        {
            LocationName = "Temuco",
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
            RetrievedAtUtc = now,
            Current = new CurrentWeather { TemperatureC = 19.0 }
        });

        var cachedService = new CachedWeatherService(inner, store, TimeSpan.FromMinutes(30), () => now);

        // 1. First fetch hits inner service and caches it.
        var first = await cachedService.GetForecastAsync(loc);
        Assert.NotNull(first);
        Assert.False(first.IsFromCache);
        Assert.Equal(1, inner.CallCount);

        // 2. Within 30 min TTL -> returns cache without calling inner service.
        now = now.AddMinutes(10);
        var second = await cachedService.GetForecastAsync(loc);
        Assert.NotNull(second);
        Assert.True(second.IsFromCache);
        Assert.Equal(1, inner.CallCount);

        // 3. After TTL expires AND inner throws (offline) -> returns stale cache gracefully.
        now = now.AddMinutes(45);
        inner.ShouldThrow = true;
        var offlineFallback = await cachedService.GetForecastAsync(loc);
        Assert.NotNull(offlineFallback);
        Assert.True(offlineFallback.IsFromCache);
        Assert.Equal(19.0, offlineFallback.Current.TemperatureC);
        Assert.Equal(2, inner.CallCount);
    }

    private sealed class InMemoryWeatherCacheStore : IWeatherCacheStore
    {
        private WeatherForecast? _cached;
        public Task<WeatherForecast?> ReadAsync(CancellationToken cancellationToken = default) => Task.FromResult(_cached);
        public Task WriteAsync(WeatherForecast forecast, CancellationToken cancellationToken = default)
        {
            _cached = forecast;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWeatherService : IWeatherService
    {
        private readonly Func<WeatherForecast> _factory;
        public int CallCount { get; private set; }
        public bool ShouldThrow { get; set; }

        public FakeWeatherService(Func<WeatherForecast> factory) => _factory = factory;

        public Task<WeatherForecast?> GetForecastAsync(
            GeoLocation location,
            int days = 7,
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (ShouldThrow)
            {
                throw new HttpRequestException("Offline");
            }

            return Task.FromResult<WeatherForecast?>(_factory());
        }
    }
}
