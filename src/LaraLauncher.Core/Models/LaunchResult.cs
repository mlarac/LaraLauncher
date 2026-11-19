namespace LaraLauncher.Core.Models;

public sealed record LaunchResult(
    bool Succeeded,
    string? ErrorMessage = null,
    bool IsTrackedProcess = false)
{
    public static LaunchResult Ok(bool isTrackedProcess = false) => new(true, null, isTrackedProcess);
    public static LaunchResult Fail(string errorMessage) => new(false, errorMessage, false);
}
