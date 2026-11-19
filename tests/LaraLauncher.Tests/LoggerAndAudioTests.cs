using System.Text;
using LaraLauncher.Infrastructure.Audio;
using LaraLauncher.Infrastructure.Persistence;

namespace LaraLauncher.Tests;

public sealed class LoggerAndAudioTests : IDisposable
{
    private readonly string _tempDir;

    public LoggerAndAudioTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"LaraLauncherLogsTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void FileAppLogger_WritesInfoAndErrorEntriesAndPrunesOldLogs()
    {
        string staleFile = Path.Combine(_tempDir, "laralauncher-20200101.log");
        File.WriteAllText(staleFile, "old log");
        File.SetLastWriteTimeUtc(staleFile, DateTime.UtcNow.AddDays(-30));

        var logger = new FileAppLogger(_tempDir, retentionDays: 7);
        logger.LogInfo("Launcher started");
        logger.LogError("Simulated failure", new InvalidOperationException("Boom"));

        Assert.False(File.Exists(staleFile));

        var logFiles = Directory.GetFiles(_tempDir, "laralauncher-*.log");
        Assert.Single(logFiles);

        string content = File.ReadAllText(logFiles[0]);
        Assert.Contains("[INFO] Launcher started", content);
        Assert.Contains("[ERROR] Simulated failure", content);
        Assert.Contains("InvalidOperationException: Boom", content);
    }

    [Fact]
    public void NavigationSoundService_GeneratesValidRiffWaveHeader()
    {
        byte[] wav = NavigationSoundService.GenerateTwoToneWav(600, 820, durationMs: 30, amplitude: 0.12);

        Assert.True(wav.Length > 44);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.Equal("data", Encoding.ASCII.GetString(wav, 36, 4));
    }

    [Fact]
    public void NavigationSoundService_MutedPlaybackDoesNotThrow()
    {
        using var service = new NavigationSoundService { IsEnabled = false };
        service.PlayMove();
        service.PlaySelect();
        service.PlayBack();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
}
