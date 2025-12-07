using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using Clipboard = System.Windows.Clipboard;

namespace ScreenCapture.Core.IO;

/// <summary>
/// Interface for clipboard operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies a bitmap to the clipboard.
    /// </summary>
    bool CopyToClipboard(Bitmap bitmap);

    /// <summary>
    /// Copies a WPF BitmapSource to the clipboard.
    /// </summary>
    bool CopyToClipboard(System.Windows.Media.Imaging.BitmapSource bitmapSource);
}

/// <summary>
/// Service for clipboard operations.
/// </summary>
public class ClipboardService : IClipboardService
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 100;

    /// <inheritdoc />
    public bool CopyToClipboard(Bitmap bitmap)
    {
        if (bitmap == null) return false;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                // Convert to WPF BitmapSource for better clipboard compatibility
                using var memoryStream = new System.IO.MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;

                var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                Clipboard.SetImage(bitmapImage);
                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Clipboard is locked by another application
                if (attempt < MaxRetries - 1)
                {
                    System.Threading.Thread.Sleep(RetryDelayMs);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool CopyToClipboard(System.Windows.Media.Imaging.BitmapSource bitmapSource)
    {
        if (bitmapSource == null) return false;

        for (int attempt = 0; attempt < MaxRetries; attempt++)
        {
            try
            {
                Clipboard.SetImage(bitmapSource);
                return true;
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Clipboard is locked by another application
                if (attempt < MaxRetries - 1)
                {
                    System.Threading.Thread.Sleep(RetryDelayMs);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        return false;
    }
}
