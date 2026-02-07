namespace BlazingSingularity.Commands;

public class AsyncRelayCommand<T> : IAsyncCommand<T>
{
    private readonly Func<T?, CancellationToken, Task>? _executeWithCancellation;
    private readonly Func<T?, Task>? _execute;
    private readonly Func<T?, bool>? _canExecute;
    private readonly Func<Task>? _notifyStateChanged;
    private bool _isLoading;
    private CancellationTokenSource? _cancellationTokenSource;

    public AsyncRelayCommand(Func<T?, Task> execute, Func<T?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public AsyncRelayCommand(
        Func<T?, Task> execute,
        Func<T?, bool>? canExecute,
        Func<Task>? notifyStateChanged
    )
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _notifyStateChanged = notifyStateChanged;
    }

    public AsyncRelayCommand(
        Func<T?, CancellationToken, Task> execute,
        Func<T?, bool>? canExecute = null
    )
    {
        _executeWithCancellation = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public AsyncRelayCommand(
        Func<T?, CancellationToken, Task> execute,
        Func<T?, bool>? canExecute,
        Func<Task>? notifyStateChanged
    )
    {
        _executeWithCancellation = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _notifyStateChanged = notifyStateChanged;
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsCancellable => _executeWithCancellation != null;

    public event EventHandler? CanExecuteChanged;

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

        return _canExecute == null || _canExecute((T?)parameter);
    }

    public bool CanExecute(T? parameter)
    {
        if (IsLoading)
            return false;

        return _canExecute == null || _canExecute(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        IsLoading = true;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            if (_executeWithCancellation != null)
            {
                await _executeWithCancellation((T?)parameter, _cancellationTokenSource.Token);
            }
            else if (_execute != null)
            {
                await _execute((T?)parameter);
            }
        }
        finally
        {
            IsLoading = false;
            if (_notifyStateChanged != null)
            {
                await _notifyStateChanged();
            }
        }
    }

    public async Task ExecuteAsync(T? parameter)
    {
        if (!CanExecute(parameter))
            return;

        IsLoading = true;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            if (_executeWithCancellation != null)
            {
                await _executeWithCancellation(parameter, _cancellationTokenSource.Token);
            }
            else if (_execute != null)
            {
                await _execute(parameter);
            }
        }
        finally
        {
            IsLoading = false;
            if (_notifyStateChanged != null)
            {
                await _notifyStateChanged();
            }
        }
    }

    public async Task<Result> TryExecuteAsync(object? parameter)
    {
        return await TryExecuteAsync((T?)parameter);
    }

    public async Task<Result> TryExecuteAsync(T? parameter)
    {
        if (!CanExecute(parameter))
            return Result.Failure(new InvalidOperationException("Command cannot execute."));

        IsLoading = true;
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            if (_executeWithCancellation != null)
            {
                await _executeWithCancellation(parameter, _cancellationTokenSource.Token);
            }
            else if (_execute != null)
            {
                await _execute(parameter);
            }

            return Result.Success();
        }
        catch (OperationCanceledException ex)
        {
            return Result.Failure(ex);
        }
        catch (Exception ex)
        {
            return Result.Failure(ex);
        }
        finally
        {
            IsLoading = false;
            if (_notifyStateChanged != null)
            {
                await _notifyStateChanged();
            }
        }
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

public class AsyncRelayCommand : AsyncRelayCommand<object>
{
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : base(_ => execute(), canExecute != null ? _ => canExecute() : null) { }

    public AsyncRelayCommand(
        Func<Task> execute,
        Func<bool>? canExecute,
        Func<Task>? notifyStateChanged
    )
        : base(_ => execute(), canExecute != null ? _ => canExecute() : null, notifyStateChanged)
    { }

    public AsyncRelayCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
        : base((_, ct) => execute(ct), canExecute != null ? _ => canExecute() : null) { }

    public AsyncRelayCommand(
        Func<CancellationToken, Task> execute,
        Func<bool>? canExecute,
        Func<Task>? notifyStateChanged
    )
        : base(
            (_, ct) => execute(ct),
            canExecute != null ? _ => canExecute() : null,
            notifyStateChanged
        )
    { }

    public async Task ExecuteAsync()
    {
        await ExecuteAsync((object?)null);
    }

    public async Task<Result> TryExecuteAsync()
    {
        return await TryExecuteAsync((object?)null);
    }

    public bool CanExecute()
    {
        return CanExecute((object?)null);
    }
}
