using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Launches desktop applications, shortcuts, URIs, and UWP apps and signals lifecycle events.
/// </summary>
public interface IAppLauncherService
{
    event EventHandler<AppTile>? ApplicationLaunched;
    event EventHandler<AppTile>? ApplicationExited;

    Task<LaunchResult> LaunchAsync(AppTile tile, CancellationToken cancellationToken = default);
}
