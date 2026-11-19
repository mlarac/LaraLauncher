using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Discovers installed applications on the host OS and extracts cached icon files.
/// </summary>
public interface IInstalledAppsScanner
{
    Task<IReadOnlyList<DiscoveredApp>> ScanAsync(CancellationToken cancellationToken = default);
    string? ExtractIconForFile(string filePath);
}
