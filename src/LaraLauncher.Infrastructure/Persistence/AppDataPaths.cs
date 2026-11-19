namespace LaraLauncher.Infrastructure.Persistence;

/// <summary>
/// Centralizes local AppData directory layout for LaraLauncher.
/// </summary>
public static class AppDataPaths
{
    public static string BaseDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LaraLauncher");

    public static string DefaultConfigFile => Path.Combine(BaseDirectory, "config.json");
    public static string IconsDirectory => Path.Combine(BaseDirectory, "icons");
    public static string CacheDirectory => Path.Combine(BaseDirectory, "cache");
    public static string LogsDirectory => Path.Combine(BaseDirectory, "logs");
}
