using System.Diagnostics;
using LaraLauncher.Core.Models;

namespace LaraLauncher.Infrastructure.Windows;

public sealed record LaunchInfoBuildResult(ProcessStartInfo? StartInfo, string? ErrorMessage)
{
    public bool IsValid => StartInfo is not null && ErrorMessage is null;
}

/// <summary>
/// Pure builder that validates an AppTile and builds a ProcessStartInfo for Windows execution.
/// </summary>
public static class AppLaunchInfoBuilder
{
    public static LaunchInfoBuildResult Build(AppTile tile, Func<string, bool>? fileExists = null)
    {
        ArgumentNullException.ThrowIfNull(tile);

        if (string.IsNullOrWhiteSpace(tile.ExecutablePath))
        {
            return new LaunchInfoBuildResult(null, $"La aplicación '{tile.Name}' no tiene una ruta configurada.");
        }

        fileExists ??= File.Exists;
        string target = tile.ExecutablePath.Trim();

        return tile.LaunchType switch
        {
            LaunchType.Uri => BuildUri(target),
            LaunchType.Uwp => BuildUwp(target),
            LaunchType.Shortcut => BuildFileBased(target, tile.Arguments, tile.Name, fileExists, requireFileExist: true),
            _ => BuildFileBased(target, tile.Arguments, tile.Name, fileExists, requireFileExist: Path.IsPathRooted(target))
        };
    }

    private static LaunchInfoBuildResult BuildUri(string target)
    {
        if (!Uri.TryCreate(target, UriKind.Absolute, out _))
        {
            return new LaunchInfoBuildResult(null, $"La dirección URI '{target}' no es válida.");
        }

        return new LaunchInfoBuildResult(new ProcessStartInfo
        {
            FileName = target,
            UseShellExecute = true
        }, null);
    }

    private static LaunchInfoBuildResult BuildUwp(string target)
    {
        string shellTarget = target.StartsWith("shell:AppsFolder\\", StringComparison.OrdinalIgnoreCase)
            ? target
            : $"shell:AppsFolder\\{target}";

        return new LaunchInfoBuildResult(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = shellTarget,
            UseShellExecute = true
        }, null);
    }

    private static LaunchInfoBuildResult BuildFileBased(
        string target,
        string? arguments,
        string displayName,
        Func<string, bool> fileExists,
        bool requireFileExist)
    {
        if (requireFileExist && !fileExists(target))
        {
            return new LaunchInfoBuildResult(null, $"No se encontró el archivo para '{displayName}': {target}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = target,
            Arguments = arguments ?? string.Empty,
            UseShellExecute = true
        };

        if (Path.IsPathRooted(target))
        {
            string? workingDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(workingDir))
            {
                startInfo.WorkingDirectory = workingDir;
            }
        }

        return new LaunchInfoBuildResult(startInfo, null);
    }
}
