namespace ExcelDbfConverter.Desktop.Abstractions;

/// <summary>页面导航服务。</summary>
public interface INavigationService
{
    object? CurrentViewModel { get; set; }

    event EventHandler? CurrentViewModelChanged;
}

/// <summary>页面导航服务实现。</summary>
public sealed class NavigationService : INavigationService
{
    private object? _currentViewModel;

    public object? CurrentViewModel
    {
        get => _currentViewModel;
        set
        {
            if (!ReferenceEquals(_currentViewModel, value))
            {
                _currentViewModel = value;
                CurrentViewModelChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? CurrentViewModelChanged;
}
