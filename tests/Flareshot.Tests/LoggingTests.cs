using Flareshot.Core.Exceptions;
using Flareshot.Core.Logging;

namespace Flareshot.Tests;

/// <summary>
/// Unit tests for custom exceptions.
/// </summary>
public class ExceptionTests
{
    [Fact]
    public void ScreenCaptureException_ShouldStoreMessage()
    {
        // Arrange & Act
        var exception = new ScreenCaptureException("Test error message");

        // Assert
        Assert.Equal("Test error message", exception.Message);
    }

    [Fact]
    public void HotkeyException_ShouldStoreHotkeyIdAndErrorCode()
    {
        // Arrange & Act
        var exception = new HotkeyException("Hotkey registration failed", hotkeyId: 1, errorCode: 1409);

        // Assert
        Assert.Equal("Hotkey registration failed", exception.Message);
        Assert.Equal(1, exception.HotkeyId);
        Assert.Equal(1409, exception.ErrorCode);
    }

    [Fact]
    public void ExportException_ShouldStoreFilePathAndType()
    {
        // Arrange & Act
        var exception = new ExportException("Failed to save", "C:\\test.png", ExportType.Png);

        // Assert
        Assert.Equal("Failed to save", exception.Message);
        Assert.Equal("C:\\test.png", exception.FilePath);
        Assert.Equal(ExportType.Png, exception.ExportType);
    }

    [Fact]
    public void CaptureException_ShouldStoreInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");
        
        // Act
        var exception = new CaptureException("Capture failed", inner);

        // Assert
        Assert.Equal("Capture failed", exception.Message);
        Assert.Equal(inner, exception.InnerException);
    }
}

/// <summary>
/// Unit tests for FileLogger.
/// </summary>
public class FileLoggerTests
{
    [Fact]
    public void FileLogger_ShouldCreateLogFolder()
    {
        // Arrange & Act
        var logger = FileLogger.Instance;

        // Assert
        Assert.True(System.IO.Directory.Exists(logger.LogFolder));
    }

    [Fact]
    public void FileLogger_ShouldHaveValidLogFilePath()
    {
        // Arrange & Act
        var logger = FileLogger.Instance;

        // Assert
        Assert.Contains("Flareshot", logger.LogFilePath);
        Assert.Contains("logs", logger.LogFilePath);
        Assert.EndsWith(".txt", logger.LogFilePath);
    }

    [Fact]
    public void FileLogger_LogFilePath_ShouldIncludeDate()
    {
        // Arrange
        var logger = FileLogger.Instance;
        var today = DateTime.Now.ToString("yyyy-MM-dd");

        // Assert
        Assert.Contains(today, logger.LogFilePath);
    }

    [Fact]
    public void FileLogger_Info_ShouldNotThrow()
    {
        // Arrange
        var logger = FileLogger.Instance;

        // Act & Assert (should not throw)
        logger.Info("Test info message", "Test");
    }

    [Fact]
    public void FileLogger_Error_WithException_ShouldNotThrow()
    {
        // Arrange
        var logger = FileLogger.Instance;
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert (should not throw)
        logger.Error("Test error", "Test", exception);
    }
}
