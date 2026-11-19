using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Services;

public readonly record struct GridNavigationResult(int NewIndex, bool ExitToSidebar);

/// <summary>
/// Pure, deterministic 2D grid navigation calculator for 10-foot tile grids.
/// </summary>
public static class GridNavigator
{
    public static GridNavigationResult Move(
        int currentIndex,
        int itemCount,
        int columnCount,
        NavigationAction action)
    {
        if (itemCount <= 0 || columnCount <= 0)
        {
            return new GridNavigationResult(-1, ExitToSidebar: action == NavigationAction.Left);
        }

        int clampedIndex = Math.Clamp(currentIndex, 0, itemCount - 1);
        int col = clampedIndex % columnCount;

        return action switch
        {
            NavigationAction.Left => col == 0
                ? new GridNavigationResult(clampedIndex, ExitToSidebar: true)
                : new GridNavigationResult(clampedIndex - 1, ExitToSidebar: false),

            NavigationAction.Right => clampedIndex + 1 < itemCount && col + 1 < columnCount
                ? new GridNavigationResult(clampedIndex + 1, ExitToSidebar: false)
                : new GridNavigationResult(clampedIndex, ExitToSidebar: false),

            NavigationAction.Up => clampedIndex - columnCount >= 0
                ? new GridNavigationResult(clampedIndex - columnCount, ExitToSidebar: false)
                : new GridNavigationResult(clampedIndex, ExitToSidebar: false),

            NavigationAction.Down => MoveDown(clampedIndex, itemCount, columnCount),

            _ => new GridNavigationResult(clampedIndex, ExitToSidebar: false)
        };
    }

    private static GridNavigationResult MoveDown(int currentIndex, int itemCount, int columnCount)
    {
        int nextRowIndex = currentIndex + columnCount;
        if (nextRowIndex < itemCount)
        {
            return new GridNavigationResult(nextRowIndex, ExitToSidebar: false);
        }

        // If we are not yet on the last row, snap to the last available tile on the partial bottom row.
        int currentRow = currentIndex / columnCount;
        int lastRow = (itemCount - 1) / columnCount;
        if (currentRow < lastRow)
        {
            return new GridNavigationResult(itemCount - 1, ExitToSidebar: false);
        }

        return new GridNavigationResult(currentIndex, ExitToSidebar: false);
    }
}
