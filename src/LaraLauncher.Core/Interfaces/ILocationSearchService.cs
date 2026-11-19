using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

public interface ILocationSearchService
{
    Task<IReadOnlyList<GeoLocation>> SearchAsync(
        string query,
        int maxResults = 6,
        CancellationToken cancellationToken = default);
}
