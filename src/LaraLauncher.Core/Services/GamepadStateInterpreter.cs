using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Services;

[Flags]
public enum GamepadButtons : ushort
{
    None = 0,
    DPadUp = 0x0001,
    DPadDown = 0x0002,
    DPadLeft = 0x0004,
    DPadRight = 0x0008,
    Start = 0x0010,
    Back = 0x0020,
    LeftShoulder = 0x0100,
    RightShoulder = 0x0200,
    A = 0x1000,
    B = 0x2000,
    X = 0x4000,
    Y = 0x8000
}

public readonly record struct GamepadSnapshot(GamepadButtons Buttons, short LeftThumbX, short LeftThumbY);

/// <summary>
/// Pure state machine that converts raw XInput gamepad snapshots into debounced NavigationActions
/// with left-stick deadzone and Roku-style directional repeat.
/// </summary>
public sealed class GamepadStateInterpreter
{
    private const short StickDeadzone = 16000;
    private static readonly TimeSpan InitialRepeatDelay = TimeSpan.FromMilliseconds(380);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(150);

    private GamepadButtons _previousButtons;
    private NavigationAction _heldDirection = NavigationAction.None;
    private TimeSpan _directionHoldElapsed = TimeSpan.Zero;
    private bool _hasRepeated;

    public IReadOnlyList<NavigationAction> Process(GamepadSnapshot snapshot, TimeSpan elapsed)
    {
        var actions = new List<NavigationAction>(2);

        // Edge-triggered face/system buttons
        GamepadButtons newlyPressed = snapshot.Buttons & ~_previousButtons;
        _previousButtons = snapshot.Buttons;

        if (newlyPressed.HasFlag(GamepadButtons.A))
        {
            actions.Add(NavigationAction.Select);
        }

        if (newlyPressed.HasFlag(GamepadButtons.B))
        {
            actions.Add(NavigationAction.Back);
        }

        if (newlyPressed.HasFlag(GamepadButtons.Y) || newlyPressed.HasFlag(GamepadButtons.Start))
        {
            actions.Add(NavigationAction.ContextMenu);
        }

        if (newlyPressed.HasFlag(GamepadButtons.Back))
        {
            actions.Add(NavigationAction.Home);
        }

        // Directional D-Pad + Left Stick with deadzone & controlled auto-repeat
        NavigationAction currentDirection = ReadDirection(snapshot);
        if (currentDirection == NavigationAction.None)
        {
            _heldDirection = NavigationAction.None;
            _directionHoldElapsed = TimeSpan.Zero;
            _hasRepeated = false;
        }
        else if (currentDirection != _heldDirection)
        {
            _heldDirection = currentDirection;
            _directionHoldElapsed = TimeSpan.Zero;
            _hasRepeated = false;
            actions.Add(currentDirection);
        }
        else
        {
            _directionHoldElapsed += elapsed;
            TimeSpan threshold = _hasRepeated ? RepeatInterval : InitialRepeatDelay;
            if (_directionHoldElapsed >= threshold)
            {
                _directionHoldElapsed -= threshold;
                _hasRepeated = true;
                actions.Add(currentDirection);
            }
        }

        return actions;
    }

    public void Reset()
    {
        _previousButtons = GamepadButtons.None;
        _heldDirection = NavigationAction.None;
        _directionHoldElapsed = TimeSpan.Zero;
        _hasRepeated = false;
    }

    private static NavigationAction ReadDirection(GamepadSnapshot snapshot)
    {
        if (snapshot.Buttons.HasFlag(GamepadButtons.DPadUp)) return NavigationAction.Up;
        if (snapshot.Buttons.HasFlag(GamepadButtons.DPadDown)) return NavigationAction.Down;
        if (snapshot.Buttons.HasFlag(GamepadButtons.DPadLeft)) return NavigationAction.Left;
        if (snapshot.Buttons.HasFlag(GamepadButtons.DPadRight)) return NavigationAction.Right;

        int absX = Math.Abs((int)snapshot.LeftThumbX);
        int absY = Math.Abs((int)snapshot.LeftThumbY);

        if (absX < StickDeadzone && absY < StickDeadzone)
        {
            return NavigationAction.None;
        }

        if (absY >= absX)
        {
            return snapshot.LeftThumbY > 0 ? NavigationAction.Up : NavigationAction.Down;
        }

        return snapshot.LeftThumbX > 0 ? NavigationAction.Right : NavigationAction.Left;
    }
}
