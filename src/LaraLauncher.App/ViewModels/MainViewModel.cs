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
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _toastTimer;

    private LauncherConfig _config = new();

    public ObservableCollection<SidebarItemViewModel> SidebarItems { get; }
    public ObservableCollection<AppTile> AppTiles { get; }

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
    private bool _isLaunchingApp;

    public bool IsHomeSection => ActiveSection == SidebarSection.Home;
    public bool IsWeatherSection => ActiveSection == SidebarSection.Weather;
    public bool IsManageAppsSection => ActiveSection == SidebarSection.ManageApps;
    public bool IsSettingsSection => ActiveSection == SidebarSection.Settings;

    public bool HasApps => AppTiles.Count > 0;
    public bool IsHomeGridVisible => IsHomeSection && HasApps;
    public bool IsHomeEmptyVisible => IsHomeSection && !HasApps;

    public LauncherConfig CurrentConfig => _config;

    public MainViewModel(IConfigRepository configRepository, IAppLauncherService appLauncherService)
    {
        _configRepository = configRepository;
        _appLauncherService = appLauncherService;

        SidebarItems = new ObservableCollection<SidebarItemViewModel>
        {
            new(SidebarSection.Home, "Inicio", "\uE80F", isSelected: true),
            new(SidebarSection.Weather, "Clima", "\uE753"),
            new(SidebarSection.ManageApps, "Gestionar Apps", "\uE71D"),
            new(SidebarSection.Settings, "Ajustes", "\uE713"),
            new(SidebarSection.Exit, "Salir", "\uE7E8")
        };

        AppTiles = new ObservableCollection<AppTile>();
        AppTiles.CollectionChanged += (_, _) => NotifyHomeVisibilityChanged();

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
        SyncAppTilesFromConfig();
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
            IsExitDialogVisible = true;
            return;
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
    }

    [RelayCommand]
    public async Task LaunchTileAsync(AppTile? tile)
    {
        if (tile is null || IsLaunchingApp)
        {
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
