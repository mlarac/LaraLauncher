using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Services;

/// <summary>
/// Pure filtering, deduplication, and visual accent assignment for discovered applications.
/// </summary>
public static class AppDiscoveryFilter
{
    private static readonly string[] ExcludedNameKeywords =
    [
        "uninstall",
        "desinstalar",
        "remove ",
        "readme",
        "léeme",
        "leeme",
        "license",
        "licencia",
        "help",
        "ayuda",
        "documentation",
        "documentación",
        "manual",
        "website",
        "web site",
        "release notes",
        "changelog",
        "update",
        "updater",
        "crash reporter",
        "bug report",
        "command prompt",
        "developer command",
        "developer powershell"
    ];

    private static readonly string[] RokuAccentPalette =
    [
        "#5A2EA6",
        "#2457A6",
        "#1E6F5C",
        "#99223B",
        "#B85C14",
        "#3A479C",
        "#7A2E8C",
        "#1A7F3E",
        "#2A6B8F"
    ];

    public static bool ShouldInclude(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        string normalized = displayName.Trim().ToLowerInvariant();
        if (normalized.Length < 2)
        {
            return false;
        }

        foreach (string keyword in ExcludedNameKeywords)
        {
            if (normalized.Contains(keyword, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    public static string GetSuggestedAccentHex(string appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            return RokuAccentPalette[0];
        }

        uint hash = 2166136261;
        foreach (char c in appName.ToLowerInvariant())
        {
            hash ^= c;
            hash *= 16777619;
        }

        return RokuAccentPalette[hash % RokuAccentPalette.Length];
    }

    public static IReadOnlyList<DiscoveredApp> DeduplicateAndSort(IEnumerable<DiscoveredApp> apps)
    {
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<DiscoveredApp>();

        foreach (var app in apps.OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            if (!ShouldInclude(app.Name) || string.IsNullOrWhiteSpace(app.ExecutablePath))
            {
                continue;
            }

            string nameKey = app.Name.Trim();
            string pathKey = app.ExecutablePath.Trim();

            if (seenNames.Contains(nameKey) || seenPaths.Contains(pathKey))
            {
                continue;
            }

            seenNames.Add(nameKey);
            seenPaths.Add(pathKey);
            result.Add(app);
        }

        return result;
    }

    public static bool MatchesPathOrName(AppTile tile, DiscoveredApp discovered)
    {
        if (string.Equals(tile.ExecutablePath, discovered.ExecutablePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(tile.Name.Trim(), discovered.Name.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
