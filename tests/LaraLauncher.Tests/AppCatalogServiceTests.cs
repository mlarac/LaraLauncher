using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Tests;

public sealed class AppCatalogServiceTests
{
    [Fact]
    public void MoveTile_ReordersItemsAndNormalizesOrder()
    {
        var tiles = new List<AppTile>
        {
            new() { Name = "A", Order = 0 },
            new() { Name = "B", Order = 1 },
            new() { Name = "C", Order = 2 }
        };

        bool moved = AppCatalogService.MoveTile(tiles, fromIndex: 0, toIndex: 2);

        Assert.True(moved);
        Assert.Equal(["B", "C", "A"], tiles.Select(t => t.Name).ToArray());
        Assert.Equal([0, 1, 2], tiles.Select(t => t.Order).ToArray());
    }

    [Fact]
    public void MoveTile_WithOutOfBoundsIndex_ReturnsFalse()
    {
        var tiles = new List<AppTile> { new() { Name = "A", Order = 0 } };

        Assert.False(AppCatalogService.MoveTile(tiles, 0, 5));
        Assert.False(AppCatalogService.MoveTile(tiles, -1, 0));
    }
}
