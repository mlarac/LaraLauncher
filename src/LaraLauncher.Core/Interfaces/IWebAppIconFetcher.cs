namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Downloads and caches a website icon (favicon) locally for WebApp tiles.
/// </summary>
public interface IWebAppIconFetcher
{
    Task<string?> FetchFaviconAsync(string normalizedUrl, CancellationToken cancellationToken = default);
}
