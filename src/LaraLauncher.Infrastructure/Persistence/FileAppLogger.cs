using System.Text;
using LaraLauncher.Core.Interfaces;

namespace LaraLauncher.Infrastructure.Persistence;

/// <summary>
/// Thread-safe daily rotating file logger stored in %AppData%\LaraLauncher\logs\.
/// </summary>
public sealed class FileAppLogger : IAppLogger
{
    private readonly string _logsDirectory;
    private readonly int _retentionDays;
    private readonly object _writeLock = new();

    public FileAppLogger(string? logsDirectory = null, int retentionDays = 7)
    {
        _logsDirectory = string.IsNullOrWhiteSpace(logsDirectory)
            ? AppDataPaths.LogsDirectory
            : logsDirectory;
        _retentionDays = Math.Max(1, retentionDays);
        PruneOldLogs();
    }

    public void LogInfo(string message) => WriteEntry("INFO", message, null);

    public void LogError(string message, Exception? exception = null) => WriteEntry("ERROR", message, exception);

    private void WriteEntry(string level, string message, Exception? exception)
    {
        try
        {
            lock (_writeLock)
            {
                Directory.CreateDirectory(_logsDirectory);
                string fileName = $"laralauncher-{DateTime.Now:yyyyMMdd}.log";
                string fullPath = Path.Combine(_logsDirectory, fileName);

                var sb = new StringBuilder();
                sb.Append('[').Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append("] [").Append(level).Append("] ");
                sb.AppendLine(message);
                if (exception is not null)
                {
                    sb.AppendLine(exception.ToString());
                }

                File.AppendAllText(fullPath, sb.ToString(), Encoding.UTF8);
            }
        }
        catch (Exception)
        {
            // Logging must never crash the launcher.
        }
    }

    public void PruneOldLogs(DateTime? referenceUtcNow = null)
    {
        try
        {
            if (!Directory.Exists(_logsDirectory))
            {
                return;
            }

            DateTime cutoff = (referenceUtcNow ?? DateTime.UtcNow).AddDays(-_retentionDays);
            foreach (string file in Directory.EnumerateFiles(_logsDirectory, "laralauncher-*.log"))
            {
                if (File.GetLastWriteTimeUtc(file) < cutoff)
                {
                    try { File.Delete(file); } catch (IOException) { }
                }
            }
        }
        catch (Exception)
        {
            // Ignore cleanup failures.
        }
    }
}
