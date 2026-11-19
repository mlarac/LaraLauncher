using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Services;

/// <summary>
/// Pure helper for maintaining normalized ordering and safe mutations on AppTile lists.
/// </summary>
public static class AppCatalogService
{
    public static void NormalizeOrder(IList<AppTile> tiles)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].Order = i;
        }
    }

    public static bool MoveTile(IList<AppTile> tiles, int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= tiles.Count ||
            toIndex < 0 || toIndex >= tiles.Count ||
            fromIndex == toIndex)
        {
            return false;
        }

        var tile = tiles[fromIndex];
        tiles.RemoveAt(fromIndex);
        tiles.Insert(toIndex, tile);
        NormalizeOrder(tiles);
        return true;
    }

    public static IReadOnlyList<AppTile> CreateWindowsStarterTiles() =>
    [
        new AppTile
        {
            Name = "Explorador de Archivos",
            ExecutablePath = "explorer.exe",
            AccentHex = "#5A2EA6",
            FallbackGlyph = "\uE8B7",
            Category = "Sistema",
            LaunchType = LaunchType.Executable,
            Order = 0
        },
        new AppTile
        {
            Name = "Calculadora",
            ExecutablePath = "calc.exe",
            AccentHex = "#1E6F5C",
            FallbackGlyph = "\uE8EF",
            Category = "Utilidades",
            LaunchType = LaunchType.Executable,
            Order = 1
        },
        new AppTile
        {
            Name = "Bloc de Notas",
            ExecutablePath = "notepad.exe",
            AccentHex = "#2457A6",
            FallbackGlyph = "\uE70F",
            Category = "Utilidades",
            LaunchType = LaunchType.Executable,
            Order = 2
        },
        new AppTile
        {
            Name = "YouTube",
            ExecutablePath = "https://www.youtube.com",
            AccentHex = "#B32028",
            FallbackGlyph = "\uE714",
            Category = "Streaming",
            LaunchType = LaunchType.Uri,
            Order = 3
        }
    ];
}
