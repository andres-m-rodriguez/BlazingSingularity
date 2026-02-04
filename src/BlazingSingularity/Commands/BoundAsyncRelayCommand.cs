namespace BlazingSingularity.Commands;

/// <summary>
/// An async command that captures values from properties/fields at execution time
/// Useful for commands that need multiple input parameters from UI bindings
/// </summary>
public class BoundAsyncRelayCommand : IAsyncCommand
{
    private readonly Func<CancellationToken, Task>? _executeWithCancellation;
    private readonly Func<Task>? _execute;
    private readonly Func<bool>? _canExecute;
    private bool _isLoading;
    private CancellationTokenSource? _cancellationTokenSource;

    public BoundAsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public BoundAsyncRelayCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute = null
    )
    {
        _executeWithCancellation = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                IsLoadingChanged?.Invoke(this, EventArgs.Empty);
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsCancellable => _executeWithCancellation != null;

    public event EventHandler? CanExecuteChanged;
    public event EventHandler? IsLoadingChanged;

    public void CancelCommand()
    {
        if (
            IsCancellable
            && _cancellationTokenSource != null
            && !_cancellationTokenSource.IsCancellationRequested
        )
        {
            _cancellationTokenSource.Cancel();
        }
    }

    public bool CanExecute(object? parameter)
    {
        if (IsLoading)
            return false;

        return _canExecute == null || _canExecute();
    }

    public bool CanExecute()
    {
        return CanExecute(null);
    }

    public async void Execute(object? parameter)
    {
        await ExecuteAsync();
    }

    public async void Execute()
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync(object? parameter)
    {
        await ExecuteAsync();
    }

    public async Task ExecuteAsync()
    {
        if (!CanExecute())
            return;

        IsLoading = true;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            if (_executeWithCancellation != null)
            {
                await _executeWithCancellation(_cancellationTokenSource.Token);
            }
            else if (_execute != null)
            {
                await _execute();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
