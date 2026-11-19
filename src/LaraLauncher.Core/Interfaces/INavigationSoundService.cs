namespace LaraLauncher.Core.Interfaces;

/// <summary>
/// Plays subtle Roku-style UI feedback sounds when navigating with keyboard, remote, or gamepad.
/// </summary>
public interface INavigationSoundService : IDisposable
{
    bool IsEnabled { get; set; }
    void PlayMove();
    void PlaySelect();
    void PlayBack();
}
