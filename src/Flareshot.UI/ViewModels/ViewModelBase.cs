using CommunityToolkit.Mvvm.ComponentModel;

namespace Flareshot.UI.ViewModels;

/// <summary>
/// Base class for all ViewModels in the application.
/// Provides change notification and common functionality.
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;

    /// <summary>
    /// Gets or sets a value indicating whether the ViewModel is busy.
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }
}
