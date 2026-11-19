using LaraLauncher.Core.Models;
using LaraLauncher.Infrastructure.Windows;

namespace LaraLauncher.Tests;

public sealed class WindowsInstalledAppsScannerTests : IDisposable
{
    private readonly string _tempRoot;

    public WindowsInstalledAppsScannerTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"LaraLauncherScanTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public async Task ScanAsync_DiscoversValidShortcuts_SkipsStartupAndUninstallers_AndInfersCategory()
    {
        string gamesDir = Path.Combine(_tempRoot, "Games");
        string startupDir = Path.Combine(_tempRoot, "Startup");
        Directory.CreateDirectory(gamesDir);
        Directory.CreateDirectory(startupDir);

        await File.WriteAllTextAsync(Path.Combine(_tempRoot, "Firefox.lnk"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(gamesDir, "Steam.lnk"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(gamesDir, "Uninstall Steam.lnk"), string.Empty);
        await File.WriteAllTextAsync(Path.Combine(startupDir, "AutoUpdate.lnk"), string.Empty);

        var iconCache = new IconCacheService(Path.Combine(_tempRoot, "icons"));
        var scanner = new WindowsInstalledAppsScanner(iconCache, [_tempRoot]);

        var results = await scanner.ScanAsync();

        Assert.Equal(2, results.Count);

        var firefox = Assert.Single(results, r => r.Name == "Firefox");
        Assert.Equal("Aplicaciones", firefox.Category);
        Assert.Equal(LaunchType.Shortcut, firefox.LaunchType);

        var steam = Assert.Single(results, r => r.Name == "Steam");
        Assert.Equal("Games", steam.Category);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
