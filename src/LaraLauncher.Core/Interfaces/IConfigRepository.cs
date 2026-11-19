using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Persists and loads launcher configuration without exposing storage details (DIP).
/// </summary>
public interface IConfigRepository
{
    string ConfigFilePath { get; }
    Task<LauncherConfig> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(LauncherConfig config, CancellationToken cancellationToken = default);
}
