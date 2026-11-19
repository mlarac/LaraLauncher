using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace LaraLauncher.App.Helpers;

/// <summary>
/// Applies a native black Win32 DWM title bar (Windows 10/11) when running in windowed mode.
/// </summary>
public static class WindowTitleBarTheme
{
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_BORDER_COLOR = 34;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

    // Win32 COLORREF uses 0x00BBGGRR format
    private const int ColorBlack = 0x00000000;
    private const int ColorWhite = 0x00FFFFFF;

    public static void ApplyBlackTitleBar(Window window)
    {
        nint hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == nint.Zero)
        {
            return;
        }

        try
        {
            int useDarkMode = 1;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
            _ = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_LEGACY, ref useDarkMode, sizeof(int));

            int captionColor = ColorBlack;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            int textColor = ColorWhite;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));

            int borderColor = ColorBlack;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
        }
        catch (Exception)
        {
            // Ignore gracefully on older OS builds that do not support DWM caption tinting.
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        nint hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
