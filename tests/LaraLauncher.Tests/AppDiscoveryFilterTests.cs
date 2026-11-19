using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Tests;

public sealed class AppDiscoveryFilterTests
{
    [Theory]
    [InlineData("Google Chrome", true)]
    [InlineData("VLC media player", true)]
    [InlineData("Steam", true)]
    [InlineData("Uninstall VLC", false)]
    [InlineData("Desinstalar Spotify", false)]
    [InlineData("Readme", false)]
    [InlineData("Release Notes", false)]
    [InlineData("Developer PowerShell for VS", false)]
    [InlineData(" ", false)]
    public void ShouldInclude_FiltersOutUtilityAndUninstallShortcuts(string name, bool expected)
    {
        Assert.Equal(expected, AppDiscoveryFilter.ShouldInclude(name));
    }

    [Fact]
    public void DeduplicateAndSort_RemovesDuplicateNamesAndPathsAndSortsAlphabetically()
    {
        var raw = new List<DiscoveredApp>
        {
            new() { Name = "VLC", ExecutablePath = @"C:\A\VLC.lnk" },
            new() { Name = "Chrome", ExecutablePath = @"C:\A\Chrome.lnk" },
            new() { Name = "vlc", ExecutablePath = @"C:\B\VLC.lnk" },
            new() { Name = "Uninstall Chrome", ExecutablePath = @"C:\A\Uninstall.lnk" }
        };

        var result = AppDiscoveryFilter.DeduplicateAndSort(raw);

        Assert.Equal(2, result.Count);
        Assert.Equal("Chrome", result[0].Name);
        Assert.Equal("VLC", result[1].Name);
    }

    [Fact]
    public void GetSuggestedAccentHex_IsDeterministicAndValidHex()
    {
        string first = AppDiscoveryFilter.GetSuggestedAccentHex("Spotify");
        string second = AppDiscoveryFilter.GetSuggestedAccentHex("spotify");

        Assert.Equal(first, second);
        Assert.StartsWith("#", first);
        Assert.Equal(7, first.Length);
    }
}
