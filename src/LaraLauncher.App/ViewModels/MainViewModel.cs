using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _toastTimer;

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

    public bool IsHomeSection => ActiveSection == SidebarSection.Home;
    public bool IsWeatherSection => ActiveSection == SidebarSection.Weather;
    public bool IsManageAppsSection => ActiveSection == SidebarSection.ManageApps;
    public bool IsSettingsSection => ActiveSection == SidebarSection.Settings;

    public MainViewModel()
    {
        SidebarItems = new ObservableCollection<SidebarItemViewModel>
        {
            new(SidebarSection.Home, "Inicio", "\uE80F", isSelected: true),
            new(SidebarSection.Weather, "Clima", "\uE753"),
            new(SidebarSection.ManageApps, "Gestionar Apps", "\uE71D"),
            new(SidebarSection.Settings, "Ajustes", "\uE713"),
            new(SidebarSection.Exit, "Salir", "\uE7E8")
        };

        // Fase 1 demo tiles; Fase 2 replaces these with JsonConfigRepository + real launcher service.
        AppTiles = new ObservableCollection<AppTile>
        {
            new() { Name = "Navegador Web", AccentHex = "#2457A6", FallbackGlyph = "\uE774", Category = "Internet", Order = 0 },
            new() { Name = "YouTube TV", AccentHex = "#B32028", FallbackGlyph = "\uE714", Category = "Streaming", Order = 1 },
            new() { Name = "Netflix", AccentHex = "#99111B", FallbackGlyph = "\uE8B2", Category = "Streaming", Order = 2 },
            new() { Name = "Spotify", AccentHex = "#1A7F3E", FallbackGlyph = "\uE8D6", Category = "Música", Order = 3 },
            new() { Name = "Steam Big Picture", AccentHex = "#1C406B", FallbackGlyph = "\uE7FC", Category = "Juegos", Order = 4 },
            new() { Name = "VLC Media Player", AccentHex = "#B85C14", FallbackGlyph = "\uE768", Category = "Multimedia", Order = 5 },
            new() { Name = "Plex / Kodi", AccentHex = "#9C6D14", FallbackGlyph = "\uE7F4", Category = "Multimedia", Order = 6 },
            new() { Name = "Explorador", AccentHex = "#5E35B1", FallbackGlyph = "\uE8B7", Category = "Sistema", Order = 7 }
        };

        UpdateClock();

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();

        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            IsStatusToastVisible = false;
        };
    }

    partial void OnActiveSectionChanged(SidebarSection value)
    {
        OnPropertyChanged(nameof(IsHomeSection));
        OnPropertyChanged(nameof(IsWeatherSection));
        OnPropertyChanged(nameof(IsManageAppsSection));
        OnPropertyChanged(nameof(IsSettingsSection));
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
    public void LaunchTile(AppTile? tile)
    {
        if (tile is null)
        {
            return;
        }

        ShowToast($"Seleccionado: {tile.Name} (la ejecución real se conecta en Fase 2)");
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

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = CultureInfo.CurrentCulture;
        CurrentTimeText = now.ToString("HH:mm", culture);
        CurrentDateText = culture.TextInfo.ToTitleCase(now.ToString("ddd, dd MMM", culture));
    }
}
