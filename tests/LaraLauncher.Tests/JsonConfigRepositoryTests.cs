using LaraLauncher.Core.Models;
using LaraLauncher.Infrastructure.Persistence;

namespace LaraLauncher.Tests;

public sealed class JsonConfigRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _configPath;

    public JsonConfigRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LaraLauncherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _configPath = Path.Combine(_tempDir, "config.json");
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ReturnsDefaultConfig()
    {
        var repo = new JsonConfigRepository(_configPath);

        var config = await repo.LoadAsync();

        Assert.NotNull(config);
        Assert.NotNull(config.Settings);
        Assert.Empty(config.Apps);
        Assert.Equal(TemperatureUnit.Celsius, config.Settings.TemperatureUnit);
    }

    [Fact]
    public async Task SaveAndLoadAsync_RoundTripsAppsAndSettings_AndNormalizesOrder()
    {
        var repo = new JsonConfigRepository(_configPath);
        var original = new LauncherConfig
        {
            Settings = new LauncherSettings
            {
                StartWithWindows = true,
                WeatherCityName = "Temuco",
                TemperatureUnit = TemperatureUnit.Celsius
            },
            Apps =
            [
                new AppTile { Name = "Segunda", ExecutablePath = "calc.exe", Order = 5 },
                new AppTile { Name = "Primera", ExecutablePath = "notepad.exe", Order = 2 }
            ]
        };

        await repo.SaveAsync(original);
        var loaded = await repo.LoadAsync();

        Assert.True(loaded.Settings.StartWithWindows);
        Assert.Equal("Temuco", loaded.Settings.WeatherCityName);
        Assert.Equal(2, loaded.Apps.Count);
        Assert.Equal("Segunda", loaded.Apps[0].Name);
        Assert.Equal(0, loaded.Apps[0].Order);
        Assert.Equal("Primera", loaded.Apps[1].Name);
        Assert.Equal(1, loaded.Apps[1].Order);

        // Ensure atomic write left no stray .tmp files.
        Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp"));
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsCorrupt_QuarantinesFileAndReturnsDefault()
    {
        await File.WriteAllTextAsync(_configPath, "{ invalid json !!!");
        var repo = new JsonConfigRepository(_configPath);

        var config = await repo.LoadAsync();

        Assert.NotNull(config);
        Assert.Empty(config.Apps);
        Assert.False(File.Exists(_configPath));
        Assert.Single(Directory.GetFiles(_tempDir, "config.json.corrupt.*"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
