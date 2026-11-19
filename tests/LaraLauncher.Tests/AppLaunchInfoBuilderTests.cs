using LaraLauncher.Core.Models;
using LaraLauncher.Infrastructure.Windows;

namespace LaraLauncher.Tests;

public sealed class AppLaunchInfoBuilderTests
{
    [Fact]
    public void Build_WithExistingRootedExecutable_SetsWorkingDirectoryAndArguments()
    {
        var tile = new AppTile
        {
            Name = "VLC",
            ExecutablePath = @"C:\Apps\VLC\vlc.exe",
            Arguments = "--fullscreen",
            LaunchType = LaunchType.Executable
        };

        var result = AppLaunchInfoBuilder.Build(tile, fileExists: _ => true);

        Assert.True(result.IsValid);
        Assert.Equal(@"C:\Apps\VLC\vlc.exe", result.StartInfo!.FileName);
        Assert.Equal(@"C:\Apps\VLC", result.StartInfo.WorkingDirectory);
        Assert.Equal("--fullscreen", result.StartInfo.Arguments);
        Assert.True(result.StartInfo.UseShellExecute);
    }

    [Fact]
    public void Build_WithMissingRootedExecutable_ReturnsError()
    {
        var tile = new AppTile
        {
            Name = "MissingApp",
            ExecutablePath = @"C:\DoesNotExist\app.exe",
            LaunchType = LaunchType.Executable
        };

        var result = AppLaunchInfoBuilder.Build(tile, fileExists: _ => false);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Build_WithBareSystemExecutable_SucceedsWithoutRequiringRootedFileCheck()
    {
        var tile = new AppTile
        {
            Name = "Notepad",
            ExecutablePath = "notepad.exe",
            LaunchType = LaunchType.Executable
        };

        var result = AppLaunchInfoBuilder.Build(tile, fileExists: _ => false);

        Assert.True(result.IsValid);
        Assert.Equal("notepad.exe", result.StartInfo!.FileName);
    }

    [Fact]
    public void Build_WithValidUri_Succeeds()
    {
        var tile = new AppTile
        {
            Name = "Steam Big Picture",
            ExecutablePath = "steam://open/bigpicture",
            LaunchType = LaunchType.Uri
        };

        var result = AppLaunchInfoBuilder.Build(tile);

        Assert.True(result.IsValid);
        Assert.Equal("steam://open/bigpicture", result.StartInfo!.FileName);
        Assert.True(result.StartInfo.UseShellExecute);
    }

    [Fact]
    public void Build_WithInvalidUri_ReturnsError()
    {
        var tile = new AppTile
        {
            Name = "Bad Link",
            ExecutablePath = "not a valid uri",
            LaunchType = LaunchType.Uri
        };

        var result = AppLaunchInfoBuilder.Build(tile);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Build_WithUwpAumid_WrapsInAppsFolderPrefix()
    {
        var tile = new AppTile
        {
            Name = "Microsoft Store",
            ExecutablePath = "Microsoft.WindowsStore_8wekyb3d8bbwe!App",
            LaunchType = LaunchType.Uwp
        };

        var result = AppLaunchInfoBuilder.Build(tile);

        Assert.True(result.IsValid);
        Assert.Equal("explorer.exe", result.StartInfo!.FileName);
        Assert.Equal(@"shell:AppsFolder\Microsoft.WindowsStore_8wekyb3d8bbwe!App", result.StartInfo.Arguments);
    }
}
