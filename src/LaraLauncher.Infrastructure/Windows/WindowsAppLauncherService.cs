using System.ComponentModel;
using System.Diagnostics;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;

namespace LaraLauncher.Infrastructure.Windows;

/// <summary>
/// Launches Windows desktop apps, shortcuts, URIs, and UWP apps and notifies when a tracked process exits.
/// </summary>
public sealed class WindowsAppLauncherService : IAppLauncherService
{
    public event EventHandler<AppTile>? ApplicationLaunched;
    public event EventHandler<AppTile>? ApplicationExited;

    public Task<LaunchResult> LaunchAsync(AppTile tile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tile);

        var buildResult = AppLaunchInfoBuilder.Build(tile);
        if (!buildResult.IsValid || buildResult.StartInfo is null)
        {
            return Task.FromResult(LaunchResult.Fail(buildResult.ErrorMessage ?? "No se pudo preparar la ejecución."));
        }

        try
        {
            var process = Process.Start(buildResult.StartInfo);
            ApplicationLaunched?.Invoke(this, tile);

            // Some shell launches (URIs handled by an open browser, UWP via explorer.exe) do not return a trackable process.
            bool canTrack = process is not null && tile.LaunchType is LaunchType.Executable or LaunchType.Shortcut;
            if (canTrack && process is not null)
            {
                _ = MonitorProcessExitAsync(process, tile);
                return Task.FromResult(LaunchResult.Ok(isTrackedProcess: true));
            }

            process?.Dispose();
            return Task.FromResult(LaunchResult.Ok(isTrackedProcess: false));
        }
        catch (Win32Exception ex)
        {
            return Task.FromResult(LaunchResult.Fail($"Error de Windows al abrir '{tile.Name}': {ex.Message}"));
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            return Task.FromResult(LaunchResult.Fail($"No se pudo abrir '{tile.Name}': {ex.Message}"));
        }
    }

    private async Task MonitorProcessExitAsync(Process process, AppTile tile)
    {
        using (process)
        {
            try
            {
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Process may already have exited or denied access after elevation.
            }
            finally
            {
                ApplicationExited?.Invoke(this, tile);
            }
        }
    }
}
