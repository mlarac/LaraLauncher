using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Infrastructure.Windows;

/// <summary>
/// Discovers installed Windows applications by scanning user and machine Start Menu shortcuts.
/// </summary>
public sealed class WindowsInstalledAppsScanner : IInstalledAppsScanner
{
    private readonly IconCacheService _iconCacheService;
    private readonly IReadOnlyList<string> _searchRoots;

    public WindowsInstalledAppsScanner(IconCacheService iconCacheService, IEnumerable<string>? customRoots = null)
    {
        _iconCacheService = iconCacheService;
        _searchRoots = customRoots?.ToList() ?? GetDefaultStartMenuRoots();
    }

    public Task<IReadOnlyList<DiscoveredApp>> ScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run<IReadOnlyList<DiscoveredApp>>(() =>
        {
            var found = new List<DiscoveredApp>();

            foreach (string root in _searchRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                {
                    continue;
                }

                ScanDirectoryRecursive(root, root, found, cancellationToken);
            }

            var cleaned = AppDiscoveryFilter.DeduplicateAndSort(found);

            foreach (var app in cleaned)
            {
                cancellationToken.ThrowIfCancellationRequested();
                app.IconPath = _iconCacheService.GetOrExtractIcon(app.ExecutablePath);
            }

            return cleaned;
        }, cancellationToken);
    }

    public string? ExtractIconForFile(string filePath)
        => _iconCacheService.GetOrExtractIcon(filePath);

    private static void ScanDirectoryRecursive(
        string root,
        string currentDir,
        List<DiscoveredApp> found,
        CancellationToken cancellationToken)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(currentDir, "*.lnk");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return;
        }

        string category = InferCategory(root, currentDir);

        foreach (string file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string name = Path.GetFileNameWithoutExtension(file).Trim();
            if (!AppDiscoveryFilter.ShouldInclude(name))
            {
                continue;
            }

            found.Add(new DiscoveredApp
            {
                Name = name,
                ExecutablePath = file,
                LaunchType = LaunchType.Shortcut,
                Category = category,
                SuggestedAccentHex = AppDiscoveryFilter.GetSuggestedAccentHex(name)
            });
        }

        IEnumerable<string> subDirs;
        try
        {
            subDirs = Directory.EnumerateDirectories(currentDir);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return;
        }

        foreach (string subDir in subDirs)
        {
            string dirName = Path.GetFileName(subDir);
            if (dirName.Equals("Startup", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("Inicio", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("Administrative Tools", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("Herramientas administrativas", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ScanDirectoryRecursive(root, subDir, found, cancellationToken);
        }
    }

    private static string InferCategory(string root, string currentDir)
    {
        if (string.Equals(root.TrimEnd('\\'), currentDir.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
        {
            return "Aplicaciones";
        }

        string folderName = Path.GetFileName(currentDir.TrimEnd('\\'));
        if (string.IsNullOrWhiteSpace(folderName) ||
            folderName.Equals("Programs", StringComparison.OrdinalIgnoreCase) ||
            folderName.Equals("Programas", StringComparison.OrdinalIgnoreCase))
        {
            return "Aplicaciones";
        }

        return folderName.Length > 20 ? "Aplicaciones" : folderName;
    }

    private static List<string> GetDefaultStartMenuRoots()
    {
        var roots = new List<string>();

        string userPrograms = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        if (!string.IsNullOrWhiteSpace(userPrograms))
        {
            roots.Add(userPrograms);
        }

        string commonPrograms = Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms);
        if (!string.IsNullOrWhiteSpace(commonPrograms))
        {
            roots.Add(commonPrograms);
        }

        return roots;
    }
}
