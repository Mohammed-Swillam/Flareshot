using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Flareshot.Core.Drawing;

namespace Flareshot.Core.IO;

/// <summary>
/// Service for exporting images with annotations.
/// </summary>
public interface IImageExporterService
{
    /// <summary>
    /// Renders a screenshot with annotations to a bitmap.
    /// </summary>
    RenderTargetBitmap RenderWithAnnotations(BitmapSource screenshot, System.Drawing.Rectangle region, IReadOnlyList<Annotation> annotations);
    
    /// <summary>
    /// Saves a bitmap as PNG.
    /// </summary>
    bool SaveAsPng(BitmapSource bitmap, string path);
    
    /// <summary>
    /// Saves a bitmap as JPEG.
    /// </summary>
    bool SaveAsJpg(BitmapSource bitmap, string path, int quality = 90);
    
    /// <summary>
    /// Gets the default save folder path.
    /// </summary>
    string GetDefaultSaveFolder();
    
    /// <summary>
    /// Generates a unique filename for a screenshot.
    /// </summary>
    string GenerateFilename(string extension = "png");
}

/// <summary>
/// Implementation of image export service.
/// </summary>
public class ImageExporterService : IImageExporterService
{
    private readonly string _defaultSaveFolder;

    public ImageExporterService()
    {
        // Default to Pictures\Screenshots folder
        _defaultSaveFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Screenshots");
    }

    /// <summary>
    /// Renders a screenshot with annotations to a bitmap.
    /// </summary>
    public RenderTargetBitmap RenderWithAnnotations(BitmapSource screenshot, System.Drawing.Rectangle region, IReadOnlyList<Annotation> annotations)
    {
        // First crop the screenshot to the region
        var croppedBitmap = CropBitmap(screenshot, region);
        if (croppedBitmap == null)
        {
            throw new InvalidOperationException("Failed to crop screenshot");
        }

        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            // Draw the cropped screenshot
            context.DrawImage(croppedBitmap, new System.Windows.Rect(0, 0, croppedBitmap.PixelWidth, croppedBitmap.PixelHeight));

            // Apply translation to render annotations at correct positions relative to crop
            context.PushTransform(new TranslateTransform(-region.X, -region.Y));

            // Draw each annotation
            foreach (var annotation in annotations)
            {
                annotation.Render(context);
            }

            context.Pop();
        }

        var renderBitmap = new RenderTargetBitmap(
            croppedBitmap.PixelWidth,
            croppedBitmap.PixelHeight,
            96, 96,
            PixelFormats.Pbgra32);

        renderBitmap.Render(visual);
        renderBitmap.Freeze();

        return renderBitmap;
    }

    /// <summary>
    /// Saves a bitmap as PNG.
    /// </summary>
    public bool SaveAsPng(BitmapSource bitmap, string path)
    {
        try
        {
            EnsureDirectoryExists(path);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new FileStream(path, FileMode.Create);
            encoder.Save(stream);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save PNG: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves a bitmap as JPEG.
    /// </summary>
    public bool SaveAsJpg(BitmapSource bitmap, string path, int quality = 90)
    {
        try
        {
            EnsureDirectoryExists(path);

            var encoder = new JpegBitmapEncoder
            {
                QualityLevel = quality
            };
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new FileStream(path, FileMode.Create);
            encoder.Save(stream);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save JPEG: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the default save folder path.
    /// </summary>
    public string GetDefaultSaveFolder()
    {
        EnsureDirectoryExists(_defaultSaveFolder + Path.DirectorySeparatorChar);
        return _defaultSaveFolder;
    }

    /// <summary>
    /// Generates a unique filename for a screenshot.
    /// </summary>
    public string GenerateFilename(string extension = "png")
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filename = $"Screenshot_{timestamp}.{extension}";
        var fullPath = Path.Combine(_defaultSaveFolder, filename);

        // Handle duplicates
        if (File.Exists(fullPath))
        {
            int counter = 1;
            do
            {
                filename = $"Screenshot_{timestamp}_{counter}.{extension}";
                fullPath = Path.Combine(_defaultSaveFolder, filename);
                counter++;
            } while (File.Exists(fullPath));
        }

        return filename;
    }

    /// <summary>
    /// Crops a bitmap to the specified region.
    /// </summary>
    private static BitmapSource? CropBitmap(BitmapSource source, System.Drawing.Rectangle region)
    {
        try
        {
            var cropRect = new System.Windows.Int32Rect(
                Math.Max(0, region.X),
                Math.Max(0, region.Y),
                Math.Min(region.Width, (int)source.PixelWidth - Math.Max(0, region.X)),
                Math.Min(region.Height, (int)source.PixelHeight - Math.Max(0, region.Y)));

            if (cropRect.Width <= 0 || cropRect.Height <= 0)
                return null;

            var croppedBitmap = new CroppedBitmap(source, cropRect);
            croppedBitmap.Freeze();
            return croppedBitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Ensures the directory for the given path exists.
    /// </summary>
    private static void EnsureDirectoryExists(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
