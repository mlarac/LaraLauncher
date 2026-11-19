namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Manages registering/unregistering LaraLauncher to start automatically with the OS.
/// </summary>
public interface IStartupService
{
    bool IsEnabled();
    void SetEnabled(bool enabled, string executablePath);
}
