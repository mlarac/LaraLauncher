using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LaraLauncher.Infrastructure.Persistence;

namespace LaraLauncher.Infrastructure.Windows;

/// <summary>
/// Extracts icons from Windows executables and shortcuts and caches them as PNG files in AppData.
/// </summary>
public sealed class IconCacheService
{
    private const uint SHGFI_ICON = 0x000000100;
    private const uint SHGFI_LARGEICON = 0x000000000;

    private readonly string _iconsDirectory;

    public IconCacheService(string? iconsDirectory = null)
    {
        _iconsDirectory = string.IsNullOrWhiteSpace(iconsDirectory)
            ? AppDataPaths.IconsDirectory
            : iconsDirectory;
    }

    public string? GetOrExtractIcon(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            Directory.CreateDirectory(_iconsDirectory);
            string cacheFile = Path.Combine(_iconsDirectory, $"{ComputeHash(filePath)}.png");
            if (File.Exists(cacheFile))
            {
                return cacheFile;
            }

            using var icon = ExtractIconFromShell(filePath) ?? TryExtractAssociatedIcon(filePath);
            if (icon is null)
            {
                return null;
            }

            using var bitmap = icon.ToBitmap();
            bitmap.Save(cacheFile, ImageFormat.Png);
            return cacheFile;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Icon? ExtractIconFromShell(string filePath)
    {
        var shfi = new SHFILEINFO();
        nint result = SHGetFileInfo(
            filePath,
            0,
            ref shfi,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            SHGFI_ICON | SHGFI_LARGEICON);

        if (result == nint.Zero || shfi.hIcon == nint.Zero)
        {
            return null;
        }

        try
        {
            using var handleIcon = Icon.FromHandle(shfi.hIcon);
            return (Icon)handleIcon.Clone();
        }
        finally
        {
            DestroyIcon(shfi.hIcon);
        }
    }

    private static Icon? TryExtractAssociatedIcon(string filePath)
    {
        try
        {
            return Icon.ExtractAssociatedIcon(filePath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string ComputeHash(string input)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public nint hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern nint SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(nint hIcon);
}
