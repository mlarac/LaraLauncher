using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;

namespace LaraLauncher.App.ViewModels;

public enum PowerActionKind
{
    ExitToDesktop,
    Sleep,
    Restart,
    Shutdown
}

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IStartupService _startupService;

    public event EventHandler<LauncherSettings>? SettingsChanged;
    public event EventHandler<PowerActionKind>? PowerActionRequested;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartWithWindowsBadge))]
    private bool _startWithWindows;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartFullscreenBadge))]
    private bool _startFullscreen = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemperatureUnitBadge))]
    private TemperatureUnit _temperatureUnit = TemperatureUnit.Celsius;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnableGamepadBadge))]
    private bool _enableGamepad = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EnableNavigationSoundsBadge))]
    private bool _enableNavigationSounds = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridColumnsBadge))]
    private int _gridColumns = 3;

    [ObservableProperty]
    private string _startupStatusHint = "Registra LaraLauncher en el inicio de sesión de Windows (sin requerir administrador).";

    public string StartWithWindowsBadge => StartWithWindows ? "Activado" : "Desactivado";
    public string StartFullscreenBadge => StartFullscreen ? "Pantalla completa" : "Modo ventana";
    public string TemperatureUnitBadge => TemperatureUnit == TemperatureUnit.Celsius ? "°C (Celsius)" : "°F (Fahrenheit)";
    public string EnableGamepadBadge => EnableGamepad ? "Activado" : "Desactivado";
    public string EnableNavigationSoundsBadge => EnableNavigationSounds ? "Activados" : "Silenciados";
    public string GridColumnsBadge => $"{GridColumns} columnas";

    public SettingsViewModel(IStartupService startupService)
    {
        _startupService = startupService;
    }

    public void LoadFromSettings(LauncherSettings settings)
    {
        StartWithWindows = _startupService.IsEnabled();
        StartFullscreen = settings.StartFullscreen;
        TemperatureUnit = settings.TemperatureUnit;
        EnableGamepad = settings.EnableGamepad;
        EnableNavigationSounds = settings.EnableNavigationSounds;
        GridColumns = Math.Clamp(settings.GridColumns, 3, 5);
    }

    public void ApplyToSettings(LauncherSettings settings)
    {
        settings.StartWithWindows = StartWithWindows;
        settings.StartFullscreen = StartFullscreen;
        settings.TemperatureUnit = TemperatureUnit;
        settings.EnableGamepad = EnableGamepad;
        settings.EnableNavigationSounds = EnableNavigationSounds;
        settings.GridColumns = GridColumns;
    }

    public bool ToggleStartWithWindows()
    {
        bool desired = !StartWithWindows;
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            StartupStatusHint = "No se pudo determinar la ruta del ejecutable actual.";
            return false;
        }

        try
        {
            _startupService.SetEnabled(desired, exePath);
            StartWithWindows = _startupService.IsEnabled();
            StartupStatusHint = StartWithWindows
                ? $"Activo en el inicio de Windows ({Path.GetFileName(exePath)})."
                : "Inicio automático con Windows desactivado.";
            NotifyChanged();
            return true;
        }
        catch (Exception ex)
        {
            StartupStatusHint = $"No se pudo actualizar el inicio automático: {ex.Message}";
            return false;
        }
    }

    public void ToggleStartFullscreen()
    {
        StartFullscreen = !StartFullscreen;
        NotifyChanged();
    }

    public void ToggleTemperatureUnit()
    {
        TemperatureUnit = TemperatureUnit == TemperatureUnit.Celsius
            ? TemperatureUnit.Fahrenheit
            : TemperatureUnit.Celsius;
        NotifyChanged();
    }

    public void ToggleEnableGamepad()
    {
        EnableGamepad = !EnableGamepad;
        NotifyChanged();
    }

    public void ToggleNavigationSounds()
    {
        EnableNavigationSounds = !EnableNavigationSounds;
        NotifyChanged();
    }

    public void CycleGridColumns()
    {
        GridColumns = GridColumns switch
        {
            3 => 4,
            4 => 5,
            _ => 3
        };
        NotifyChanged();
    }

    public void RequestPowerAction(PowerActionKind action)
    {
        PowerActionRequested?.Invoke(this, action);
    }

    private void NotifyChanged()
    {
        var snapshot = new LauncherSettings();
        ApplyToSettings(snapshot);
        SettingsChanged?.Invoke(this, snapshot);
    }
}
