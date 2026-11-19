namespace LaraLauncher.Core.Models;

/// <summary>
/// Root persisted state of LaraLauncher. Managed exclusively through the graphical UI.
/// </summary>
public sealed class LauncherConfig
{
    public int SchemaVersion { get; set; } = 1;
    public LauncherSettings Settings { get; set; } = new();
    public List<AppTile> Apps { get; set; } = new();
}
