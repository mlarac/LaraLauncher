namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Lightweight logging abstraction for runtime diagnostics and unhandled exception recording.
/// </summary>
public interface IAppLogger
{
    void LogInfo(string message);
    void LogError(string message, Exception? exception = null);
}
