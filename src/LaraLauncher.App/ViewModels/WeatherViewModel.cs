using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.ViewModels;

public sealed partial class WeatherViewModel : ObservableObject
{
    private readonly IWeatherService _weatherService;
    private readonly ILocationSearchService _locationSearchService;
    private readonly DispatcherTimer _refreshTimer;

    private GeoLocation? _activeLocation;
    private WeatherForecast? _lastForecast;
    private TemperatureUnit _unit = TemperatureUnit.Celsius;

    public ObservableCollection<DailyForecastItemViewModel> ForecastDays { get; } = new();
    public ObservableCollection<GeoLocation> CitySearchResults { get; } = new();

    public event EventHandler<GeoLocation>? LocationSelected;

    [ObservableProperty]
    private string _headerIcon = "⛅";

    [ObservableProperty]
    private string _headerSummaryText = "Configurar clima";

    [ObservableProperty]
    private string _locationTitle = "Sin ciudad configurada";

    [ObservableProperty]
    private string _currentTempText = "--°";

    [ObservableProperty]
    private string _currentIcon = "⛅";

    [ObservableProperty]
    private string _currentConditionText = "Selecciona tu ciudad para ver el pronóstico";

    [ObservableProperty]
    private string _currentDetailsText = string.Empty;

    [ObservableProperty]
    private string _lastUpdatedText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasForecast;

    [ObservableProperty]
    private bool _isCityPickerVisible = true;

    [ObservableProperty]
    private string _citySearchQuery = string.Empty;

    [ObservableProperty]
    private string _citySearchStatus = "Escribe el nombre de tu ciudad (ej. Temuco, Santiago, Madrid) y presiona Buscar.";

    [ObservableProperty]
    private bool _isSearchingCities;

    public bool HasCitySearchResults => CitySearchResults.Count > 0;

    public WeatherViewModel(IWeatherService weatherService, ILocationSearchService locationSearchService)
    {
        _weatherService = weatherService;
        _locationSearchService = locationSearchService;

        CitySearchResults.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasCitySearchResults));

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _refreshTimer.Tick += async (_, _) =>
        {
            if (_activeLocation is not null)
            {
                await LoadForecastAsync(forceRefresh: false);
            }
        };
        _refreshTimer.Start();
    }

    public async Task InitializeFromSettingsAsync(LauncherSettings settings)
    {
        _unit = settings.TemperatureUnit;

        if (!string.IsNullOrWhiteSpace(settings.WeatherCityName) &&
            settings.WeatherLatitude.HasValue &&
            settings.WeatherLongitude.HasValue)
        {
            _activeLocation = new GeoLocation
            {
                Name = settings.WeatherCityName,
                Latitude = settings.WeatherLatitude.Value,
                Longitude = settings.WeatherLongitude.Value
            };

            LocationTitle = _activeLocation.Name;
            IsCityPickerVisible = false;
            await LoadForecastAsync(forceRefresh: false);
        }
        else
        {
            _activeLocation = null;
            HasForecast = false;
            IsCityPickerVisible = true;
            HeaderIcon = "⛅";
            HeaderSummaryText = "Configurar clima";
        }
    }

    public void UpdateTemperatureUnit(TemperatureUnit unit)
    {
        _unit = unit;
        if (_lastForecast is not null)
        {
            ApplyForecastToUi(_lastForecast);
        }
    }

    public void ToggleCityPicker(bool visible)
    {
        IsCityPickerVisible = visible;
        if (visible)
        {
            CitySearchStatus = "Escribe el nombre de tu ciudad y presiona Buscar.";
        }
    }

    public async Task SearchCitiesAsync()
    {
        string query = CitySearchQuery?.Trim() ?? string.Empty;
        if (query.Length < 2)
        {
            CitySearchStatus = "Escribe al menos 2 letras para buscar una ciudad.";
            return;
        }

        IsSearchingCities = true;
        CitySearchStatus = $"Buscando '{query}'...";
        CitySearchResults.Clear();

        try
        {
            var results = await _locationSearchService.SearchAsync(query, maxResults: 6);
            foreach (var loc in results)
            {
                CitySearchResults.Add(loc);
            }

            CitySearchStatus = results.Count > 0
                ? $"Se encontraron {results.Count} resultados. Selecciona tu ciudad:"
                : $"No se encontraron ciudades para '{query}'.";
        }
        catch (Exception)
        {
            CitySearchStatus = "No se pudo conectar al servicio de búsqueda. Verifica tu conexión a internet.";
        }
        finally
        {
            IsSearchingCities = false;
        }
    }

    public async Task SelectLocationAsync(GeoLocation location)
    {
        _activeLocation = location;
        LocationTitle = location.DisplayName;
        IsCityPickerVisible = false;
        CitySearchResults.Clear();
        CitySearchQuery = string.Empty;

        LocationSelected?.Invoke(this, location);
        await LoadForecastAsync(forceRefresh: true);
    }

    public async Task LoadForecastAsync(bool forceRefresh)
    {
        if (_activeLocation is null || IsLoading)
        {
            return;
        }

        IsLoading = true;
        try
        {
            var forecast = await _weatherService.GetForecastAsync(_activeLocation, days: 7, forceRefresh: forceRefresh);
            if (forecast is null)
            {
                HasForecast = _lastForecast is not null;
                HeaderSummaryText = $"{_activeLocation.Name} (sin conexión)";
                LastUpdatedText = "Sin conexión y sin datos en caché todavía.";
                return;
            }

            _lastForecast = forecast;
            ApplyForecastToUi(forecast);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyForecastToUi(WeatherForecast forecast)
    {
        HasForecast = true;
        LocationTitle = string.IsNullOrWhiteSpace(forecast.LocationName)
            ? (_activeLocation?.Name ?? "Clima")
            : forecast.LocationName;

        string shortCity = LocationTitle.Split(',')[0].Trim();
        string formattedCurrent = TemperatureFormatter.Format(forecast.Current.TemperatureC, _unit);
        string formattedFeels = TemperatureFormatter.Format(forecast.Current.ApparentTemperatureC, _unit);

        HeaderIcon = forecast.Current.ConditionIcon;
        HeaderSummaryText = $"{formattedCurrent} · {shortCity}";

        CurrentIcon = forecast.Current.ConditionIcon;
        CurrentTempText = formattedCurrent;
        CurrentConditionText = forecast.Current.ConditionText;
        CurrentDetailsText = $"Sensación {formattedFeels}  •  Humedad {forecast.Current.HumidityPercent}%  •  Viento {Math.Round(forecast.Current.WindSpeedKmh)} km/h";

        string timeLocal = forecast.RetrievedAtUtc.ToLocalTime().ToString("HH:mm");
        LastUpdatedText = forecast.IsFromCache
            ? $"Actualizado {timeLocal} (caché local)"
            : $"Actualizado {timeLocal}";

        ForecastDays.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var day in forecast.DailyDays.Take(7))
        {
            ForecastDays.Add(new DailyForecastItemViewModel(day, _unit, today));
        }
    }
}
