using LaraLauncher.Core.Interfaces;
using Microsoft.Win32;

namespace LaraLauncher.Infrastructure.Windows;

/// <summary>
/// Manages per-user Windows auto-start via HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Requires no administrator privileges.
/// </summary>
public sealed class WindowsStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppValueName = "LaraLauncher";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(AppValueName) is string value && !string.IsNullOrWhiteSpace(value);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void SetEnabled(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (key is null)
        {
            return;
        }

        if (enabled)
        {
            string command = FormatStartupCommand(executablePath);
            key.SetValue(AppValueName, command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(AppValueName, throwOnMissingValue: false);
        }
    }

    public static string FormatStartupCommand(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("Executable path cannot be empty.", nameof(executablePath));
        }

        string trimmed = executablePath.Trim().Trim('"');
        return $"\"{trimmed}\"";
    }
}
