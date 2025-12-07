using System.Windows;
using System.Windows.Controls;

using UserControl = System.Windows.Controls.UserControl;

namespace Flareshot.UI.Controls;

/// <summary>
/// Toolbar for capture actions (copy, save, upload).
/// </summary>
public partial class ActionToolbar : UserControl
{
    /// <summary>
    /// Event raised when copy is requested.
    /// </summary>
    public event EventHandler? CopyRequested;

    /// <summary>
    /// Event raised when save is requested.
    /// </summary>
    public event EventHandler? SaveRequested;

    /// <summary>
    /// Event raised when upload is requested.
    /// </summary>
    public event EventHandler? UploadRequested;

    /// <summary>
    /// Event raised when close is requested.
    /// </summary>
    public event EventHandler? CloseRequested;

    public ActionToolbar()
    {
        InitializeComponent();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        CopyRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UploadButton_Click(object sender, RoutedEventArgs e)
    {
        UploadRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ShareButton_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder for share functionality
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder for print functionality
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets visibility of the upload button.
    /// </summary>
    public void ShowUploadButton(bool show)
    {
        UploadButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }
}
