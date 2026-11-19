using LaraLauncher.Core.Models;

namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Contract for external input sources (e.g., XInput gamepads, HID/CEC remotes).
/// Allows adding new hardware input providers without modifying UI code (OCP).
/// </summary>
public interface IInputProvider : IDisposable
{
    event EventHandler<NavigationAction>? ActionReceived;
    void Start();
    void Stop();
}
