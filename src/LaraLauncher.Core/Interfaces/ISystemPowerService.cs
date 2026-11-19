namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Provides OS-level power actions (Sleep, Restart, Shutdown) for couch/HTPC usage.
/// </summary>
public interface ISystemPowerService
{
    void Sleep();
    void Restart();
    void Shutdown();
}
