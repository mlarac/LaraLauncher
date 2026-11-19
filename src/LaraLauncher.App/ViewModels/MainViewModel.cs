using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IConfigRepository _configRepository;
    private readonly IAppLauncherService _appLauncherService;
    private readonly ISystemPowerService _powerService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _toastTimer;

    private LauncherConfig _config = new();
    private PowerActionKind _pendingPowerAction = PowerActionKind.ExitToDesktop;

    public AppManagerViewModel AppManager { get; }
    public WeatherViewModel Weather { get; }
    public SettingsViewModel Settings { get; }
    public ObservableCollection<SidebarItemViewModel> SidebarItems { get; }
    public ObservableCollection<AppTile> AppTiles { get; }

    public event EventHandler<LauncherSettings>? RuntimePreferencesUpdated;

    public IReadOnlyList<string> AccentSwatches { get; } =
    [
        "#5A2EA6",
        "#2457A6",
        "#1E6F5C",
        "#99223B",
        "#B85C14",
        "#3A479C"
    ];

    [ObservableProperty]
    private SidebarSection _activeSection = SidebarSection.Home;

    [ObservableProperty]
    private string _sectionTitle = "Inicio";

    [ObservableProperty]
    private string _currentTimeText = string.Empty;

    [ObservableProperty]
    private string _currentDateText = string.Empty;

    [ObservableProperty]
    private string _weatherSummaryText = "-- °C  |  Clima (Fase 4)";

    [ObservableProperty]
    private string _statusToast = string.Empty;

    [ObservableProperty]
    private bool _isStatusToastVisible;

    [ObservableProperty]
    private bool _isExitDialogVisible;

    [ObservableProperty]
    private string _powerDialogTitle = "¿Salir de LaraLauncher?";

    [ObservableProperty]
    private string _powerDialogSubtitle = "Volverás al escritorio de Windows.";

    [ObservableProperty]
    private string _powerDialogConfirmText = "Salir";

    [ObservableProperty]
    private int _gridColumns = 3;

    [ObservableProperty]
    private bool _isLaunchingApp;

    // Context menu, Move mode & Edit overlay state
    [ObservableProperty]
    private AppTile? _contextTile;

    [ObservableProperty]
    private bool _isTileContextMenuVisible;

    [ObservableProperty]
    private bool _isMoveMode;

    [ObservableProperty]
    private string _moveModeBannerText = string.Empty;

    [ObservableProperty]
    private bool _isEditTileDialogVisible;

    [ObservableProperty]
    private string _editTileName = string.Empty;

    [ObservableProperty]
    private string _editTileCategory = string.Empty;

    [ObservableProperty]
    private string _editTileAccentHex = "#5A2EA6";

    public bool IsHomeSection => ActiveSection == SidebarSection.Home;
    public bool IsWeatherSection => ActiveSection == SidebarSection.Weather;
    public bool IsManageAppsSection => ActiveSection == SidebarSection.ManageApps;
    public bool IsSettingsSection => ActiveSection == SidebarSection.Settings;

    public bool HasApps => AppTiles.Count > 0;
    public bool IsHomeGridVisible => IsHomeSection && HasApps;
    public bool IsHomeEmptyVisible => IsHomeSection && !HasApps;

    public LauncherConfig CurrentConfig => _config;

    public MainViewModel(
        IConfigRepository configRepository,
        IAppLauncherService appLauncherService,
        ISystemPowerService powerService,
        AppManagerViewModel appManager,
        WeatherViewModel weather,
        SettingsViewModel settings)
    {
        _configRepository = configRepository;
        _appLauncherService = appLauncherService;
        _powerService = powerService;
        AppManager = appManager;
        Weather = weather;
        Settings = settings;

        Weather.LocationSelected += OnWeatherLocationSelected;
        Settings.SettingsChanged += OnSettingsChanged;
        Settings.PowerActionRequested += OnPowerActionRequested;

        SidebarItems = new ObservableCollection<SidebarItemViewModel>
        {
            new(SidebarSection.Home, "Inicio", "\uE80F", isSelected: true),
            new(SidebarSection.Weather, "Clima", "\uE753"),
            new(SidebarSection.ManageApps, "Gestionar Apps", "\uE71D"),
            new(SidebarSection.Settings, "Ajustes", "\uE713"),
            new(SidebarSection.Exit, "Salir", "\uE7E8")
        };

        AppTiles = new ObservableCollection<AppTile>();
        AppTiles.CollectionChanged += (_, _) =>
        {
            NotifyHomeVisibilityChanged();
            AppManager.SyncSelections(AppTiles);
        };

        UpdateClock();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            IsStatusToastVisible = false;
        };
    }

    public async Task InitializeAsync()
    {
        _config = await _configRepository.LoadAsync();
        GridColumns = Math.Clamp(_config.Settings.GridColumns, 3, 5);
        Settings.LoadFromSettings(_config.Settings);
        RuntimePreferencesUpdated?.Invoke(this, _config.Settings);
        SyncAppTilesFromConfig();
        _ = Weather.InitializeFromSettingsAsync(_config.Settings);
    }

    private async void OnSettingsChanged(object? sender, LauncherSettings updated)
    {
        Settings.ApplyToSettings(_config.Settings);
        GridColumns = Math.Clamp(_config.Settings.GridColumns, 3, 5);
        Weather.UpdateTemperatureUnit(_config.Settings.TemperatureUnit);
        RuntimePreferencesUpdated?.Invoke(this, _config.Settings);
        await SaveConfigAsync();
        ShowToast("Ajustes guardados.");
    }

    private void OnPowerActionRequested(object? sender, PowerActionKind action)
    {
        OpenPowerDialog(action);
    }

    public void OpenPowerDialog(PowerActionKind action)
    {
        _pendingPowerAction = action;
        (PowerDialogTitle, PowerDialogSubtitle, PowerDialogConfirmText) = action switch
        {
            PowerActionKind.Sleep => ("¿Suspender el equipo?", "Windows entrará en modo suspensión.", "Suspender"),
            PowerActionKind.Restart => ("¿Reiniciar el equipo?", "Se cerrarán las aplicaciones abiertas y el PC se reiniciará.", "Reiniciar"),
            PowerActionKind.Shutdown => ("¿Apagar el equipo?", "Se cerrarán las aplicaciones abiertas y el PC se apagará.", "Apagar"),
            _ => ("¿Salir de LaraLauncher?", "Volverás al escritorio de Windows.", "Salir")
        };

        IsExitDialogVisible = true;
    }

    public bool ExecutePendingPowerAction()
    {
        IsExitDialogVisible = false;

        switch (_pendingPowerAction)
        {
            case PowerActionKind.Sleep:
                _powerService.Sleep();
                return false;
            case PowerActionKind.Restart:
                _powerService.Restart();
                return false;
            case PowerActionKind.Shutdown:
                _powerService.Shutdown();
                return false;
            default:
                return true; // Caller shuts down WPF application
        }
    }

    private async void OnWeatherLocationSelected(object? sender, GeoLocation location)
    {
        _config.Settings.WeatherCityName = location.DisplayName;
        _config.Settings.WeatherLatitude = location.Latitude;
        _config.Settings.WeatherLongitude = location.Longitude;
        await SaveConfigAsync();
        ShowToast($"Ciudad del clima guardada: {location.Name}");
    }

    public async Task SaveConfigAsync()
    {
        _config.Apps = AppTiles.ToList();
        AppCatalogService.NormalizeOrder(_config.Apps);
        await _configRepository.SaveAsync(_config);
    }

    partial void OnActiveSectionChanged(SidebarSection value)
    {
        OnPropertyChanged(nameof(IsHomeSection));
        OnPropertyChanged(nameof(IsWeatherSection));
        OnPropertyChanged(nameof(IsManageAppsSection));
        OnPropertyChanged(nameof(IsSettingsSection));
        NotifyHomeVisibilityChanged();
    }

    [RelayCommand]
    public void SelectSection(SidebarSection section)
    {
        if (section == SidebarSection.Exit)
        {
            OpenPowerDialog(PowerActionKind.ExitToDesktop);
            return;
        }

        if (IsMoveMode && section != SidebarSection.Home)
        {
            IsMoveMode = false;
        }

        ActiveSection = section;

        foreach (var item in SidebarItems)
        {
            item.IsSelected = item.Section == section;
        }

        SectionTitle = section switch
        {
            SidebarSection.Home => "Inicio",
            SidebarSection.Weather => "Clima y Pronóstico",
            SidebarSection.ManageApps => "Gestionar Aplicaciones",
            SidebarSection.Settings => "Ajustes",
            _ => "LaraLauncher"
        };

        if (section == SidebarSection.ManageApps)
        {
            _ = AppManager.EnsureScannedAsync(AppTiles);
        }
    }

    [RelayCommand]
    public async Task LaunchTileAsync(AppTile? tile)
    {
        if (tile is null || IsLaunchingApp)
        {
            return;
        }

        if (IsMoveMode)
        {
            await FinishMoveModeAsync();
            return;
        }

        IsLaunchingApp = true;
        try
        {
            var result = await _appLauncherService.LaunchAsync(tile);
            if (result.Succeeded)
            {
                ShowToast($"Abriendo {tile.Name}...");
            }
            else
            {
                ShowToast(result.ErrorMessage ?? $"No se pudo abrir {tile.Name}.");
            }
        }
        finally
        {
            IsLaunchingApp = false;
        }
    }

    [RelayCommand]
    public async Task AddStarterAppsAsync()
    {
        if (AppTiles.Count > 0)
        {
            return;
        }

        foreach (var tile in AppCatalogService.CreateWindowsStarterTiles())
        {
            AppTiles.Add(tile);
        }

        await SaveConfigAsync();
        ShowToast("Se agregaron 4 aplicaciones básicas y se guardó la configuración.");
    }

    [RelayCommand]
    public void OpenManageApps()
    {
        SelectSection(SidebarSection.ManageApps);
    }

    public async Task ToggleDiscoveredAppAsync(SelectableAppViewModel item)
    {
        var existing = AppTiles.FirstOrDefault(t => AppDiscoveryFilter.MatchesPathOrName(t, item.App));
        if (existing is not null)
        {
            AppTiles.Remove(existing);
            item.IsSelected = false;
            await SaveConfigAsync();
            ShowToast($"Quitado de Inicio: {item.Name}");
        }
        else
        {
            var tile = AppManager.CreateTileFromDiscovered(item.App, AppTiles.Count);
            AppTiles.Add(tile);
            item.IsSelected = true;
            await SaveConfigAsync();
            ShowToast($"Agregado a Inicio: {item.Name}");
        }
    }

    public async Task AddManualAppFileAsync(string filePath)
    {
        if (AppTiles.Any(t => string.Equals(t.ExecutablePath, filePath, StringComparison.OrdinalIgnoreCase)))
        {
            ShowToast("Esa aplicación ya está en tu pantalla de Inicio.");
            return;
        }

        var tile = AppManager.CreateTileFromManualFile(filePath, AppTiles.Count);
        AppTiles.Add(tile);
        await SaveConfigAsync();
        ShowToast($"Agregado manualmente: {tile.Name}");
    }

    public void OpenTileContextMenu(AppTile tile)
    {
        ContextTile = tile;
        IsTileContextMenuVisible = true;
    }

    public void CloseTileContextMenu()
    {
        IsTileContextMenuVisible = false;
    }

    public void StartMoveMode()
    {
        if (ContextTile is null)
        {
            return;
        }

        IsTileContextMenuVisible = false;
        IsMoveMode = true;
        MoveModeBannerText = $"Modo Mover: usa las Flechas para reubicar '{ContextTile.Name}' y presiona Enter o Esc para guardar.";
    }

    public bool TrySwapTileInMoveMode(int fromIndex, int toIndex)
    {
        if (!IsMoveMode || fromIndex == toIndex)
        {
            return false;
        }

        var list = AppTiles.ToList();
        if (!AppCatalogService.MoveTile(list, fromIndex, toIndex))
        {
            return false;
        }

        AppTiles.Move(fromIndex, toIndex);
        AppCatalogService.NormalizeOrder(AppTiles);
        return true;
    }

    public async Task FinishMoveModeAsync()
    {
        if (!IsMoveMode)
        {
            return;
        }

        IsMoveMode = false;
        MoveModeBannerText = string.Empty;
        await SaveConfigAsync();
        ShowToast("Nuevo orden guardado.");
    }

    public void OpenEditTileDialog()
    {
        if (ContextTile is null)
        {
            return;
        }

        IsTileContextMenuVisible = false;
        EditTileName = ContextTile.Name;
        EditTileCategory = ContextTile.Category;
        EditTileAccentHex = ContextTile.AccentHex;
        IsEditTileDialogVisible = true;
    }

    public void SelectEditAccent(string hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            EditTileAccentHex = hex;
        }
    }

    public async Task SaveEditTileAsync()
    {
        if (ContextTile is null)
        {
            IsEditTileDialogVisible = false;
            return;
        }

        int index = AppTiles.IndexOf(ContextTile);
        if (index < 0)
        {
            IsEditTileDialogVisible = false;
            return;
        }

        string trimmedName = string.IsNullOrWhiteSpace(EditTileName) ? ContextTile.Name : EditTileName.Trim();
        string trimmedCategory = string.IsNullOrWhiteSpace(EditTileCategory) ? "General" : EditTileCategory.Trim();

        var updated = new AppTile
        {
            Id = ContextTile.Id,
            Name = trimmedName,
            ExecutablePath = ContextTile.ExecutablePath,
            Arguments = ContextTile.Arguments,
            IconPath = ContextTile.IconPath,
            AccentHex = string.IsNullOrWhiteSpace(EditTileAccentHex) ? ContextTile.AccentHex : EditTileAccentHex,
            FallbackGlyph = ContextTile.FallbackGlyph,
            Category = trimmedCategory,
            LaunchType = ContextTile.LaunchType,
            Order = index
        };

        AppTiles[index] = updated;
        ContextTile = updated;
        IsEditTileDialogVisible = false;

        await SaveConfigAsync();
        ShowToast($"Cambios guardados para '{updated.Name}'.");
    }

    public void CancelEditTile()
    {
        IsEditTileDialogVisible = false;
    }

    public async Task RemoveContextTileAsync()
    {
        if (ContextTile is null)
        {
            return;
        }

        string removedName = ContextTile.Name;
        AppTiles.Remove(ContextTile);
        ContextTile = null;
        IsTileContextMenuVisible = false;

        await SaveConfigAsync();
        ShowToast($"'{removedName}' quitado de Inicio.");
    }

    [RelayCommand]
    public void CancelExit()
    {
        IsExitDialogVisible = false;
    }

    public void ShowToast(string message)
    {
        StatusToast = message;
        IsStatusToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Start();
    }

    private void SyncAppTilesFromConfig()
    {
        AppTiles.Clear();
        foreach (var tile in _config.Apps.OrderBy(a => a.Order))
        {
            AppTiles.Add(tile);
        }

        AppManager.SyncSelections(AppTiles);
        NotifyHomeVisibilityChanged();
    }

    private void NotifyHomeVisibilityChanged()
    {
        OnPropertyChanged(nameof(HasApps));
        OnPropertyChanged(nameof(IsHomeGridVisible));
        OnPropertyChanged(nameof(IsHomeEmptyVisible));
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = CultureInfo.CurrentCulture;
        CurrentTimeText = now.ToString("HH:mm", culture);
        CurrentDateText = culture.TextInfo.ToTitleCase(now.ToString("ddd, dd MMM", culture));
    }
}
