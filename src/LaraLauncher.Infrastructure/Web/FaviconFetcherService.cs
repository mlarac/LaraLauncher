using System.Security.Cryptography;
using System.Text;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Infrastructure.Persistence;

namespace LaraLauncher.Infrastructure.Web;

/// <summary>
/// Downloads and caches high-resolution website favicons locally for WebApp tiles.
/// Fails silently when offline so WebApp creation always succeeds with a fallback glyph.
/// </summary>
public sealed class FaviconFetcherService : IWebAppIconFetcher
{
    private readonly HttpClient _httpClient;
    private readonly string _iconsDirectory;

    public FaviconFetcherService(HttpClient httpClient, string? iconsDirectory = null)
    {
        _httpClient = httpClient;
        _iconsDirectory = string.IsNullOrWhiteSpace(iconsDirectory)
            ? AppDataPaths.IconsDirectory
            : iconsDirectory;
    }

    public async Task<string?> FetchFaviconAsync(string normalizedUrl, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) || string.IsNullOrWhiteSpace(uri.Host))
        {
            return null;
        }

        string host = uri.Host.ToLowerInvariant();
        if (host == "localhost")
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(_iconsDirectory);
            string cachePath = Path.Combine(_iconsDirectory, $"web_{ComputeHash(host)}.png");
            if (File.Exists(cachePath))
            {
                return cachePath;
            }

            string faviconEndpoint = $"https://www.google.com/s2/favicons?domain={Uri.EscapeDataString(host)}&sz=128";
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _httpClient.GetAsync(faviconEndpoint, timeoutCts.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            byte[] bytes = await response.Content.ReadAsByteArrayAsync(timeoutCts.Token).ConfigureAwait(false);
            if (bytes.Length < 64)
            {
                return null;
            }

            await File.WriteAllBytesAsync(cachePath, bytes, timeoutCts.Token).ConfigureAwait(false);
            return cachePath;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ComputeHash(string input)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
