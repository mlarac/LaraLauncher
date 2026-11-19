using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;
using LaraLauncher.Infrastructure.Windows;

namespace LaraLauncher.Tests;

public sealed class GamepadAndStartupTests
{
    [Fact]
    public void GamepadInterpreter_ButtonA_IsEdgeTriggered()
    {
        var interpreter = new GamepadStateInterpreter();
        var pressed = new GamepadSnapshot(GamepadButtons.A, 0, 0);
        var released = new GamepadSnapshot(GamepadButtons.None, 0, 0);

        Assert.Equal([NavigationAction.Select], interpreter.Process(pressed, TimeSpan.FromMilliseconds(16)));
        Assert.Empty(interpreter.Process(pressed, TimeSpan.FromMilliseconds(16)));
        Assert.Empty(interpreter.Process(released, TimeSpan.FromMilliseconds(16)));
        Assert.Equal([NavigationAction.Select], interpreter.Process(pressed, TimeSpan.FromMilliseconds(16)));
    }

    [Fact]
    public void GamepadInterpreter_MapsB_Y_Start_And_BackButtons()
    {
        var interpreter = new GamepadStateInterpreter();

        Assert.Equal([NavigationAction.Back], interpreter.Process(new(GamepadButtons.B, 0, 0), TimeSpan.FromMilliseconds(16)));
        Assert.Equal([NavigationAction.ContextMenu], interpreter.Process(new(GamepadButtons.Y, 0, 0), TimeSpan.FromMilliseconds(16)));
        Assert.Equal([NavigationAction.ContextMenu], interpreter.Process(new(GamepadButtons.Start, 0, 0), TimeSpan.FromMilliseconds(16)));
        Assert.Equal([NavigationAction.Home], interpreter.Process(new(GamepadButtons.Back, 0, 0), TimeSpan.FromMilliseconds(16)));
    }

    [Fact]
    public void GamepadInterpreter_LeftStickRespectsDeadzoneAndRepeatsAfterDelay()
    {
        var interpreter = new GamepadStateInterpreter();

        // Inside deadzone -> ignored
        Assert.Empty(interpreter.Process(new(GamepadButtons.None, 9000, -8000), TimeSpan.FromMilliseconds(16)));

        // Tilted right beyond deadzone -> immediate Right action
        var right = new GamepadSnapshot(GamepadButtons.None, 24000, 1000);
        Assert.Equal([NavigationAction.Right], interpreter.Process(right, TimeSpan.FromMilliseconds(16)));

        // Still under initial 380ms delay -> no repeat yet
        Assert.Empty(interpreter.Process(right, TimeSpan.FromMilliseconds(200)));

        // Passes initial delay -> repeats once
        Assert.Equal([NavigationAction.Right], interpreter.Process(right, TimeSpan.FromMilliseconds(200)));

        // Subsequent repeat interval is 150ms
        Assert.Empty(interpreter.Process(right, TimeSpan.FromMilliseconds(100)));
        Assert.Equal([NavigationAction.Right], interpreter.Process(right, TimeSpan.FromMilliseconds(60)));
    }

    [Fact]
    public void StartupCommandFormatter_QuotesPathWithSpacesCleanly()
    {
        string cmd = WindowsStartupService.FormatStartupCommand(@"C:\Program Files\LaraLauncher\LaraLauncher.exe");
        Assert.Equal("\"C:\\Program Files\\LaraLauncher\\LaraLauncher.exe\"", cmd);

        string alreadyQuoted = WindowsStartupService.FormatStartupCommand("\"C:\\Apps\\LaraLauncher.exe\"");
        Assert.Equal("\"C:\\Apps\\LaraLauncher.exe\"", alreadyQuoted);

        Assert.Throws<ArgumentException>(() => WindowsStartupService.FormatStartupCommand("   "));
    }
}
