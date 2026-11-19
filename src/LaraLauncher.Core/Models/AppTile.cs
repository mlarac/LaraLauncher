namespace LaraLauncher.Core.Models;

public enum LaunchType
{
    Executable,
    Shortcut,
    Uri,
    Uwp
}

/// <summary>
/// Represents a single application tile shown in the launcher grid.
/// </summary>
public sealed class AppTile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? IconPath { get; set; }
    public string AccentHex { get; set; } = "#5A2EA6";
    public string FallbackGlyph { get; set; } = "\uE74C";
    public string Category { get; set; } = "General";
    public LaunchType LaunchType { get; set; } = LaunchType.Executable;
    public int Order { get; set; }
}
