using System.Diagnostics;
using System.Runtime.InteropServices;
using LaraLauncher.Core.Interfaces;
using LaraLauncher.Core.Models;
using LaraLauncher.Core.Services;

namespace LaraLauncher.Infrastructure.Input;

/// <summary>
/// Lightweight XInput gamepad provider (Xbox & compatible controllers) using direct Win32 P/Invoke.
/// Polls only while started (paused automatically when LaraLauncher loses focus or runs an external app).
/// </summary>
public sealed class XInputGamepadProvider : IInputProvider
{
    private const uint ErrorSuccess = 0;
    private const int MaxControllers = 4;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(16);

    private readonly GamepadStateInterpreter[] _interpreters =
    [
        new(), new(), new(), new()
    ];

    private readonly object _syncRoot = new();
    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;
    private bool _xinputUnavailable;
    private bool _useFallbackDll;
    private bool _disposed;

    public bool IsEnabled { get; set; } = true;

    public event EventHandler<NavigationAction>? ActionReceived;

    public void Start()
    {
        lock (_syncRoot)
        {
            if (_disposed || _xinputUnavailable || !IsEnabled || _pollTask is not null)
            {
                return;
            }

            foreach (var interpreter in _interpreters)
            {
                interpreter.Reset();
            }

            _pollCts = new CancellationTokenSource();
            _pollTask = Task.Run(() => PollLoopAsync(_pollCts.Token));
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        lock (_syncRoot)
        {
            cts = _pollCts;
            _pollCts = null;
            _pollTask = null;
        }

        if (cts is not null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                cts.Dispose();
            }
        }
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        TimeSpan lastTick = stopwatch.Elapsed;

        try
        {
            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!IsEnabled)
                {
                    continue;
                }

                TimeSpan now = stopwatch.Elapsed;
                TimeSpan elapsed = now - lastTick;
                lastTick = now;

                for (uint slot = 0; slot < MaxControllers; slot++)
                {
                    if (!TryGetState(slot, out var state))
                    {
                        _interpreters[slot].Reset();
                        continue;
                    }

                    var snapshot = new GamepadSnapshot(
                        (GamepadButtons)state.Gamepad.wButtons,
                        state.Gamepad.sThumbLX,
                        state.Gamepad.sThumbLY);

                    var actions = _interpreters[slot].Process(snapshot, elapsed);
                    foreach (var action in actions)
                    {
                        ActionReceived?.Invoke(this, action);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Clean stop requested.
        }
    }

    private bool TryGetState(uint userIndex, out XINPUT_STATE state)
    {
        state = default;
        if (_xinputUnavailable)
        {
            return false;
        }

        try
        {
            uint result = _useFallbackDll
                ? XInputGetStateFallback(userIndex, ref state)
                : XInputGetStatePrimary(userIndex, ref state);

            return result == ErrorSuccess;
        }
        catch (DllNotFoundException)
        {
            if (!_useFallbackDll)
            {
                _useFallbackDll = true;
                return TryGetState(userIndex, out state);
            }

            _xinputUnavailable = true;
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            _xinputUnavailable = true;
            return false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        Stop();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_GAMEPAD
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XINPUT_STATE
    {
        public uint dwPacketNumber;
        public XINPUT_GAMEPAD Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetStatePrimary(uint dwUserIndex, ref XINPUT_STATE pState);

    [DllImport("xinput9_1_0.dll", EntryPoint = "XInputGetState")]
    private static extern uint XInputGetStateFallback(uint dwUserIndex, ref XINPUT_STATE pState);
}
