using System.Diagnostics;
using System.Runtime.InteropServices;
using LaraLauncher.Core.Interfaces;

namespace LaraLauncher.Infrastructure.Windows;

/// <summary>
/// Executes Windows power management actions (Sleep, Restart, Shutdown).
/// </summary>
public sealed class WindowsPowerService : ISystemPowerService
{
    public void Sleep()
    {
        SetSuspendState(hibernate: false, forceCritical: false, disableWakeEvent: false);
    }

    public void Restart()
    {
        RunShutdownCommand("/r /t 0");
    }

    public void Shutdown()
    {
        RunShutdownCommand("/s /t 0");
    }

    private static void RunShutdownCommand(string arguments)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = arguments,
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
}
