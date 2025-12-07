using System.IO;

namespace ScreenCapture.Core.Logging;

/// <summary>
/// Simple file logger for the application.
/// </summary>
public interface ILogger
{
    void Debug(string message, string component = "App");
    void Info(string message, string component = "App");
    void Warning(string message, string component = "App");
    void Error(string message, string component = "App", Exception? exception = null);
}

/// <summary>
/// Log level enumeration.
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// File-based logger implementation.
/// </summary>
public class FileLogger : ILogger, IDisposable
{
    private readonly string _logFolder;
    private readonly string _logFilePath;
    private readonly object _lock = new();
    private readonly LogLevel _minimumLevel;
    private bool _disposed;

    /// <summary>
    /// Gets the singleton instance of the logger.
    /// </summary>
    public static FileLogger Instance { get; } = new();

    public FileLogger(LogLevel minimumLevel = LogLevel.Info)
    {
        _minimumLevel = minimumLevel;
        
        // Log to %APPDATA%\Flareshot\logs\
        _logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Flareshot",
            "logs");

        EnsureLogFolderExists();

        // Create log file with date
        var fileName = $"log_{DateTime.Now:yyyy-MM-dd}.txt";
        _logFilePath = Path.Combine(_logFolder, fileName);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    public void Debug(string message, string component = "App")
    {
        Log(LogLevel.Debug, message, component);
    }

    /// <summary>
    /// Logs an info message.
    /// </summary>
    public void Info(string message, string component = "App")
    {
        Log(LogLevel.Info, message, component);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warning(string message, string component = "App")
    {
        Log(LogLevel.Warning, message, component);
    }

    /// <summary>
    /// Logs an error message with optional exception.
    /// </summary>
    public void Error(string message, string component = "App", Exception? exception = null)
    {
        if (exception != null)
        {
            message = $"{message}\n  Exception: {exception.GetType().Name}: {exception.Message}\n  StackTrace: {exception.StackTrace}";
        }
        Log(LogLevel.Error, message, component);
    }

    /// <summary>
    /// Core logging method.
    /// </summary>
    private void Log(LogLevel level, string message, string component)
    {
        if (level < _minimumLevel) return;
        if (_disposed) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpperInvariant().PadRight(7);
        var logLine = $"[{timestamp}] [{levelStr}] [{component}] {message}";

        // Write to file (thread-safe)
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                // Fail silently - logging should never crash the app
            }
        }

        // Also output to debug console
        System.Diagnostics.Debug.WriteLine(logLine);
    }

    /// <summary>
    /// Ensures the log folder exists.
    /// </summary>
    private void EnsureLogFolderExists()
    {
        try
        {
            if (!Directory.Exists(_logFolder))
            {
                Directory.CreateDirectory(_logFolder);
            }
        }
        catch
        {
            // Fail silently
        }
    }

    /// <summary>
    /// Gets the path to the current log file.
    /// </summary>
    public string LogFilePath => _logFilePath;

    /// <summary>
    /// Gets the path to the log folder.
    /// </summary>
    public string LogFolder => _logFolder;

    /// <summary>
    /// Cleans up old log files (older than specified days).
    /// </summary>
    public void CleanupOldLogs(int daysToKeep = 7)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var logFiles = Directory.GetFiles(_logFolder, "log_*.txt");

            foreach (var file in logFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Fail silently
                    }
                }
            }
        }
        catch
        {
            // Fail silently
        }
    }

    public void Dispose()
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
