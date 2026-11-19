using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Tests;

public sealed class GridNavigatorTests
{
    [Fact]
    public void Move_LeftOnFirstColumn_ExitsToSidebar()
    {
        var result = GridNavigator.Move(currentIndex: 3, itemCount: 8, columnCount: 3, NavigationAction.Left);

        Assert.True(result.ExitToSidebar);
        Assert.Equal(3, result.NewIndex);
    }

    [Fact]
    public void Move_LeftOnMiddleColumn_MovesLeftOneTile()
    {
        var result = GridNavigator.Move(currentIndex: 4, itemCount: 8, columnCount: 3, NavigationAction.Left);

        Assert.False(result.ExitToSidebar);
        Assert.Equal(3, result.NewIndex);
    }

    [Fact]
    public void Move_RightInsideRow_AdvancesIndex()
    {
        var result = GridNavigator.Move(currentIndex: 1, itemCount: 8, columnCount: 3, NavigationAction.Right);

        Assert.False(result.ExitToSidebar);
        Assert.Equal(2, result.NewIndex);
    }

    [Fact]
    public void Move_RightAtRowEdge_DoesNotWrapToNextRow()
    {
        var result = GridNavigator.Move(currentIndex: 2, itemCount: 8, columnCount: 3, NavigationAction.Right);

        Assert.False(result.ExitToSidebar);
        Assert.Equal(2, result.NewIndex);
    }

    [Fact]
    public void Move_DownIntoPartialBottomRow_SnapsToLastItem()
    {
        // 8 items in 3 columns:
        // Row 0: 0 1 2
        // Row 1: 3 4 5
        // Row 2: 6 7
        var result = GridNavigator.Move(currentIndex: 5, itemCount: 8, columnCount: 3, NavigationAction.Down);

        Assert.False(result.ExitToSidebar);
        Assert.Equal(7, result.NewIndex);
    }

    [Fact]
    public void Move_UpFromSecondRow_MovesToSameColumnAbove()
    {
        var result = GridNavigator.Move(currentIndex: 4, itemCount: 8, columnCount: 3, NavigationAction.Up);

        Assert.False(result.ExitToSidebar);
        Assert.Equal(1, result.NewIndex);
    }

    [Fact]
    public void Move_OnEmptyGrid_LeftExitsToSidebar()
    {
        var result = GridNavigator.Move(currentIndex: 0, itemCount: 0, columnCount: 3, NavigationAction.Left);

        Assert.True(result.ExitToSidebar);
        Assert.Equal(-1, result.NewIndex);
    }
}
