using System.Text.Json;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;

namespace LaraLauncher.Infrastructure.Persistence;

public sealed class JsonWeatherCacheStore : IWeatherCacheStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _cacheFilePath;
    private readonly SemaphoreSlim _ioLock = new(1, 1);

    public JsonWeatherCacheStore(string? cacheFilePath = null)
    {
        _cacheFilePath = string.IsNullOrWhiteSpace(cacheFilePath)
            ? Path.Combine(AppDataPaths.CacheDirectory, "weather-cache.json")
            : cacheFilePath;
    }

    public async Task<WeatherForecast?> ReadAsync(CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_cacheFilePath))
            {
                return null;
            }

            try
            {
                await using var stream = File.OpenRead(_cacheFilePath);
                return await JsonSerializer.DeserializeAsync<WeatherForecast>(
                    stream,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return null;
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }

    public async Task WriteAsync(WeatherForecast forecast, CancellationToken cancellationToken = default)
    {
        await _ioLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string? directory = Path.GetDirectoryName(_cacheFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = $"{_cacheFilePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                {
                    await JsonSerializer.SerializeAsync(stream, forecast, SerializerOptions, cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                File.Move(tempPath, _cacheFilePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch (IOException) { }
                }
            }
        }
        finally
        {
            _ioLock.Release();
        }
    }
}
