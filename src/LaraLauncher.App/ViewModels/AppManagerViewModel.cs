using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.App.ViewModels;

public sealed partial class AppManagerViewModel : ObservableObject
{
    private readonly IInstalledAppsScanner _scanner;
    private readonly List<SelectableAppViewModel> _allDiscovered = new();
    private bool _hasScannedOnce;

    public ObservableCollection<SelectableAppViewModel> FilteredApps { get; } = new();

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private int _activeAppsCount;

    [ObservableProperty]
    private int _discoveredCount;

    [ObservableProperty]
    private string _statusMessage = "Marca las aplicaciones que deseas ver en la pantalla de inicio.";

    public bool HasFilteredApps => FilteredApps.Count > 0;
    public bool ShowEmptyFilterMessage => !IsScanning && FilteredApps.Count == 0 && _hasScannedOnce;

    public AppManagerViewModel(IInstalledAppsScanner scanner)
    {
        _scanner = scanner;
        FilteredApps.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasFilteredApps));
            OnPropertyChanged(nameof(ShowEmptyFilterMessage));
        };
    }

    partial void OnSearchQueryChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmptyFilterMessage));
    }

    public async Task EnsureScannedAsync(IReadOnlyCollection<AppTile> currentTiles)
    {
        ActiveAppsCount = currentTiles.Count;

        if (_hasScannedOnce)
        {
            SyncSelections(currentTiles);
            return;
        }

        await RefreshScanAsync(currentTiles);
    }

    public async Task RefreshScanAsync(IReadOnlyCollection<AppTile> currentTiles)
    {
        if (IsScanning)
        {
            return;
        }

        IsScanning = true;
        StatusMessage = "Buscando aplicaciones instaladas en Windows...";
        try
        {
            var discovered = await _scanner.ScanAsync();
            _allDiscovered.Clear();

            foreach (var app in discovered)
            {
                bool isPinned = currentTiles.Any(t => AppDiscoveryFilter.MatchesPathOrName(t, app));
                _allDiscovered.Add(new SelectableAppViewModel(app, isPinned));
            }

            _hasScannedOnce = true;
            DiscoveredCount = _allDiscovered.Count;
            ActiveAppsCount = currentTiles.Count;
            ApplyFilter();

            StatusMessage = DiscoveredCount > 0
                ? $"Se detectaron {DiscoveredCount} aplicaciones. Presiona Enter o haz clic para marcarlas."
                : "No se detectaron accesos directos en el Menú Inicio. Puedes agregar un archivo .exe manualmente.";
        }
        finally
        {
            IsScanning = false;
        }
    }

    public void SyncSelections(IReadOnlyCollection<AppTile> currentTiles)
    {
        ActiveAppsCount = currentTiles.Count;

        foreach (var item in _allDiscovered)
        {
            item.IsSelected = currentTiles.Any(t => AppDiscoveryFilter.MatchesPathOrName(t, item.App));
        }
    }

    public AppTile CreateTileFromDiscovered(DiscoveredApp discovered, int order)
    {
        return new AppTile
        {
            Name = discovered.Name,
            ExecutablePath = discovered.ExecutablePath,
            IconPath = discovered.IconPath,
            AccentHex = discovered.SuggestedAccentHex,
            FallbackGlyph = "\uE74C",
            Category = discovered.Category,
            LaunchType = discovered.LaunchType,
            Order = order
        };
    }

    public AppTile CreateTileFromManualFile(string filePath, int order)
    {
        string name = Path.GetFileNameWithoutExtension(filePath).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Aplicación";
        }

        string? iconPath = _scanner.ExtractIconForFile(filePath);
        bool isShortcut = filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase);

        return new AppTile
        {
            Name = name,
            ExecutablePath = filePath,
            IconPath = iconPath,
            AccentHex = AppDiscoveryFilter.GetSuggestedAccentHex(name),
            FallbackGlyph = "\uE74C",
            Category = "Personalizada",
            LaunchType = isShortcut ? LaunchType.Shortcut : LaunchType.Executable,
            Order = order
        };
    }

    private void ApplyFilter()
    {
        string query = SearchQuery?.Trim() ?? string.Empty;
        FilteredApps.Clear();

        IEnumerable<SelectableAppViewModel> matches = string.IsNullOrEmpty(query)
            ? _allDiscovered
            : _allDiscovered.Where(a =>
                a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.Category.Contains(query, StringComparison.OrdinalIgnoreCase));

        // Show pinned apps first only when not searching? Keep alphabetical so items don't jump unexpectedly when toggled!
        foreach (var item in matches)
        {
            FilteredApps.Add(item);
        }
    }
}
