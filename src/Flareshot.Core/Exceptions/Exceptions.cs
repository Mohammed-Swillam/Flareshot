namespace Flareshot.Core.Exceptions;

/// <summary>
/// Base exception for ScreenCapture application.
/// </summary>
public class ScreenCaptureException : Exception
{
    public ScreenCaptureException(string message) : base(message) { }
    public ScreenCaptureException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when hotkey operations fail.
/// </summary>
public class HotkeyException : ScreenCaptureException
{
    public int HotkeyId { get; }
    public int ErrorCode { get; }

    public HotkeyException(string message) : base(message) { }
    
    public HotkeyException(string message, int hotkeyId, int errorCode) 
        : base(message)
    {
        HotkeyId = hotkeyId;
        ErrorCode = errorCode;
    }
    
    public HotkeyException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when export operations fail.
/// </summary>
public class ExportException : ScreenCaptureException
{
    public string? FilePath { get; }
    public ExportType ExportType { get; }

    public ExportException(string message) : base(message) { }
    
    public ExportException(string message, string filePath, ExportType exportType) 
        : base(message)
    {
        FilePath = filePath;
        ExportType = exportType;
    }
    
    public ExportException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>
/// Type of export operation.
/// </summary>
public enum ExportType
{
    Clipboard,
    Png,
    Jpeg,
    Unknown
}

/// <summary>
/// Exception thrown when screen capture fails.
/// </summary>
public class CaptureException : ScreenCaptureException
{
    public CaptureException(string message) : base(message) { }
    public CaptureException(string message, Exception innerException) : base(message, innerException) { }
}
