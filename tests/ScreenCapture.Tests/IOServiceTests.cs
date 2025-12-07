using ScreenCapture.Core.IO;

namespace ScreenCapture.Tests;

/// <summary>
/// Unit tests for ImageExporterService.
/// </summary>
public class ImageExporterServiceTests
{
    [Fact]
    public void GenerateFilename_ShouldIncludeTimestamp()
    {
        // Arrange
        var exporter = new ImageExporterService();

        // Act
        var filename = exporter.GenerateFilename("png");

        // Assert
        Assert.StartsWith("Screenshot_", filename);
        Assert.EndsWith(".png", filename);
        Assert.Contains("-", filename); // Contains date separators
    }

    [Fact]
    public void GenerateFilename_ShouldSupportDifferentExtensions()
    {
        // Arrange
        var exporter = new ImageExporterService();

        // Act
        var pngFilename = exporter.GenerateFilename("png");
        var jpgFilename = exporter.GenerateFilename("jpg");

        // Assert
        Assert.EndsWith(".png", pngFilename);
        Assert.EndsWith(".jpg", jpgFilename);
    }

    [Fact]
    public void GetDefaultSaveFolder_ShouldReturnPicturesScreenshotsPath()
    {
        // Arrange
        var exporter = new ImageExporterService();

        // Act
        var folder = exporter.GetDefaultSaveFolder();

        // Assert
        Assert.Contains("Pictures", folder);
        Assert.Contains("Screenshots", folder);
    }

    [Fact]
    public void GetDefaultSaveFolder_ShouldCreateDirectoryIfNotExists()
    {
        // Arrange
        var exporter = new ImageExporterService();

        // Act
        var folder = exporter.GetDefaultSaveFolder();

        // Assert
        Assert.True(System.IO.Directory.Exists(folder));
    }
}

/// <summary>
/// Unit tests for ClipboardService.
/// Note: These tests are limited because clipboard operations require STA thread.
/// </summary>
public class ClipboardServiceTests
{
    [Fact]
    public void CopyToClipboard_WithNullBitmap_ShouldReturnFalse()
    {
        // Arrange
        var clipboardService = new ClipboardService();

        // Act
        var result = clipboardService.CopyToClipboard((System.Drawing.Bitmap)null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CopyToClipboard_WithNullBitmapSource_ShouldReturnFalse()
    {
        // Arrange
        var clipboardService = new ClipboardService();

        // Act
        var result = clipboardService.CopyToClipboard((System.Windows.Media.Imaging.BitmapSource)null!);

        // Assert
        Assert.False(result);
    }
}
