using System.Text.Json;
using System.Text.Json.Serialization;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Infrastructure.Persistence;

/// <summary>
/// Persists LauncherConfig as JSON using atomic temp-file replacement and corrupt-file recovery.
/// </summary>
public sealed class JsonConfigRepository : IConfigRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public string ConfigFilePath { get; }

    public JsonConfigRepository(string? configFilePath = null)
    {
        ConfigFilePath = string.IsNullOrWhiteSpace(configFilePath)
            ? AppDataPaths.DefaultConfigFile
            : configFilePath;
    }

    public async Task<LauncherConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(ConfigFilePath))
            {
                return new LauncherConfig();
            }

            try
            {
                await using var stream = File.OpenRead(ConfigFilePath);
                var config = await JsonSerializer.DeserializeAsync<LauncherConfig>(
                    stream,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);

                if (config is null)
                {
                    return new LauncherConfig();
                }

                config.Settings ??= new LauncherSettings();
                config.Apps ??= new List<AppTile>();
                config.Apps = config.Apps.OrderBy(a => a.Order).ToList();
                AppCatalogService.NormalizeOrder(config.Apps);

                return config;
            }
            catch (JsonException)
            {
                QuarantineCorruptFile();
                return new LauncherConfig();
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task SaveAsync(LauncherConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string? directory = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AppCatalogService.NormalizeOrder(config.Apps);

            string tempPath = $"{ConfigFilePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(
                    tempPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(
                        stream,
                        config,
                        SerializerOptions,
                        cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, ConfigFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    TryDeleteFile(tempPath);
                }
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    private void QuarantineCorruptFile()
    {
        try
        {
            string backupPath = $"{ConfigFilePath}.corrupt.{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(ConfigFilePath, backupPath, overwrite: true);
        }
        catch (IOException)
        {
            // Ignore backup errors so startup remains resilient.
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup of temp file.
        }
    }
}
