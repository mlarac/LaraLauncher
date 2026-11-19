namespace LaraLauncher.Core.Models;

/// <summary>
/// Semantic navigation actions unified across keyboard, IR/RF remotes, and gamepads.
/// </summary>
public enum NavigationAction
{
    None = 0,
    Up,
    Down,
    Left,
    Right,
    Select,
    Back,
    ContextMenu,
    Home
}
