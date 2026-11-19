namespace LaraLauncher.Core.Models;

/// <summary>
/// Represents an installed application discovered on the OS for graphical selection.
/// </summary>
public sealed class DiscoveredApp
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? IconPath { get; set; }
    public LaunchType LaunchType { get; set; } = LaunchType.Shortcut;
    public string Category { get; set; } = "Aplicaciones";
    public string SuggestedAccentHex { get; set; } = "#5A2EA6";
}
