using System.Globalization;
using System.Text.Json;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Infrastructure.Weather;

/// <summary>
/// Free, keyless Open-Meteo client supporting current weather, multi-day forecasts, and city search.
/// </summary>
public sealed class OpenMeteoWeatherService : IWeatherService, ILocationSearchService
{
    private readonly HttpClient _httpClient;

    public OpenMeteoWeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<WeatherForecast?> GetForecastAsync(
        GeoLocation location,
        int days = 7,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(location);

        int clampedDays = Math.Clamp(days, 1, 14);
        string lat = location.Latitude.ToString("0.####", CultureInfo.InvariantCulture);
        string lon = location.Longitude.ToString("0.####", CultureInfo.InvariantCulture);

        string url =
            $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
            "&current=temperature_2m,apparent_temperature,relative_humidity_2m,wind_speed_10m,weather_code" +
            "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
            $"&timezone=auto&forecast_days={clampedDays}";

        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseForecastJson(json, location);
    }

    public async Task<IReadOnlyList<GeoLocation>> SearchAsync(
        string query,
        int maxResults = 6,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return Array.Empty<GeoLocation>();
        }

        string encoded = Uri.EscapeDataString(query.Trim());
        int count = Math.Clamp(maxResults, 1, 10);
        string url = $"https://geocoding-api.open-meteo.com/v1/search?name={encoded}&count={count}&language=es&format=json";

        using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return ParseGeocodingJson(json);
    }

    public static WeatherForecast? ParseForecastJson(string json, GeoLocation location)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("current", out var currentEl) ||
            !root.TryGetProperty("daily", out var dailyEl))
        {
            return null;
        }

        int currentCode = GetInt(currentEl, "weather_code");
        var currentCondition = WmoWeatherMapper.Map(currentCode);

        var current = new CurrentWeather
        {
            TemperatureC = GetDouble(currentEl, "temperature_2m"),
            ApparentTemperatureC = GetDouble(currentEl, "apparent_temperature"),
            HumidityPercent = GetInt(currentEl, "relative_humidity_2m"),
            WindSpeedKmh = GetDouble(currentEl, "wind_speed_10m"),
            WeatherCode = currentCode,
            ConditionText = currentCondition.Description,
            ConditionIcon = currentCondition.Icon
        };

        var dailyDays = new List<DailyForecast>();
        if (dailyEl.TryGetProperty("time", out var timesEl) && timesEl.ValueKind == JsonValueKind.Array)
        {
            var codesEl = dailyEl.TryGetProperty("weather_code", out var c) ? c : default;
            var maxEl = dailyEl.TryGetProperty("temperature_2m_max", out var mx) ? mx : default;
            var minEl = dailyEl.TryGetProperty("temperature_2m_min", out var mn) ? mn : default;
            var precipEl = dailyEl.TryGetProperty("precipitation_probability_max", out var pr) ? pr : default;

            int length = timesEl.GetArrayLength();
            for (int i = 0; i < length; i++)
            {
                string? dateStr = timesEl[i].GetString();
                if (!DateOnly.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    continue;
                }

                int code = GetArrayInt(codesEl, i);
                var condition = WmoWeatherMapper.Map(code);

                dailyDays.Add(new DailyForecast
                {
                    Date = date,
                    WeatherCode = code,
                    ConditionText = condition.Description,
                    ConditionIcon = condition.Icon,
                    MaxTemperatureC = GetArrayDouble(maxEl, i),
                    MinTemperatureC = GetArrayDouble(minEl, i),
                    PrecipitationProbabilityPercent = GetArrayInt(precipEl, i)
                });
            }
        }

        return new WeatherForecast
        {
            LocationName = string.IsNullOrWhiteSpace(location.DisplayName) ? location.Name : location.DisplayName,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            RetrievedAtUtc = DateTimeOffset.UtcNow,
            IsFromCache = false,
            Current = current,
            DailyDays = dailyDays
        };
    }

    public static IReadOnlyList<GeoLocation> ParseGeocodingJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var resultsEl) ||
            resultsEl.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<GeoLocation>();
        }

        var list = new List<GeoLocation>();
        foreach (var item in resultsEl.EnumerateArray())
        {
            string name = GetString(item, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            list.Add(new GeoLocation
            {
                Name = name,
                Admin1 = GetString(item, "admin1"),
                Country = GetString(item, "country"),
                Latitude = GetDouble(item, "latitude"),
                Longitude = GetDouble(item, "longitude")
            });
        }

        return list;
    }

    private static string GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? string.Empty : string.Empty;

    private static double GetDouble(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static int GetInt(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? (int)Math.Round(v.GetDouble()) : 0;

    private static double GetArrayDouble(JsonElement arr, int index) =>
        arr.ValueKind == JsonValueKind.Array && index < arr.GetArrayLength() && arr[index].ValueKind == JsonValueKind.Number
            ? arr[index].GetDouble()
            : 0;

    private static int GetArrayInt(JsonElement arr, int index) =>
        arr.ValueKind == JsonValueKind.Array && index < arr.GetArrayLength() && arr[index].ValueKind == JsonValueKind.Number
            ? (int)Math.Round(arr[index].GetDouble())
            : 0;
}
