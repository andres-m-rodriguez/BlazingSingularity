namespace BlazingSingularity.Commands;

public static class RelayCommands
{
    public static SyncCommandBuilder Create() => new SyncCommandBuilder();

    public static SyncCommandBuilder<T> Create<T>() => new SyncCommandBuilder<T>();

    public static AsyncCommandBuilder CreateAsync() => new AsyncCommandBuilder();

    public static AsyncCommandBuilder<T> CreateAsync<T>() => new AsyncCommandBuilder<T>();
}

public class SyncCommandBuilder
{
    private Action? _execute;
    private Func<bool>? _canExecute;

    public SyncCommandBuilder WithExecute(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        return this;
    }

    public SyncCommandBuilder WithCanExecute(Func<bool> canExecute)
    {
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        return this;
    }

    public RelayCommand Build()
    {
        if (_execute == null)
            throw new InvalidOperationException(
                "Execute action must be provided before building the command."
            );

        return new RelayCommand(_execute, _canExecute);
    }
}

public class SyncCommandBuilder<T>
{
    private Action<T?>? _execute;
    private Func<T?, bool>? _canExecute;

    public SyncCommandBuilder<T> WithExecute(Action<T?> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        return this;
    }

    public SyncCommandBuilder<T> WithCanExecute(Func<T?, bool> canExecute)
    {
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        return this;
    }

    public RelayCommand<T> Build()
    {
        if (_execute == null)
            throw new InvalidOperationException(
                "Execute action must be provided before building the command."
            );

        return new RelayCommand<T>(_execute, _canExecute);
    }
}

public class AsyncCommandBuilder
{
    private Func<Task>? _execute;
    private Func<CancellationToken, Task>? _executeWithCancellation;
    private Func<bool>? _canExecute;
    private bool _useBoundCommand;

    public AsyncCommandBuilder WithExecute(Func<Task> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _executeWithCancellation = null;
        return this;
    }

    public AsyncCommandBuilder WithCancellation(Func<CancellationToken, Task> execute)
    {
        _executeWithCancellation = execute ?? throw new ArgumentNullException(nameof(execute));
        _execute = null;
        return this;
    }

    public AsyncCommandBuilder WithCanExecute(Func<bool> canExecute)
    {
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        return this;
    }

    /// <summary>
    /// Creates a bound command that captures property values at execution time.
    /// Useful for commands with multiple filter properties.
    /// </summary>
    public AsyncCommandBuilder AsBound()
    {
        _useBoundCommand = true;
        return this;
    }

    public IAsyncCommand Build()
    {
        if (_execute == null && _executeWithCancellation == null)
            throw new InvalidOperationException(
                "Execute action must be provided before building the command."
            );

        if (_useBoundCommand)
        {
            if (_executeWithCancellation != null)
                return new BoundAsyncRelayCommand(_executeWithCancellation, _canExecute);
            if (_execute != null)
                return new BoundAsyncRelayCommand(_execute, _canExecute);
        }

        if (_executeWithCancellation != null)
            return new AsyncRelayCommand(_executeWithCancellation, _canExecute);

        return new AsyncRelayCommand(_execute!, _canExecute);
    }
}

public class AsyncCommandBuilder<T>
{
    private Func<T?, Task>? _execute;
    private Func<T?, CancellationToken, Task>? _executeWithCancellation;
    private Func<T?, bool>? _canExecute;

    public AsyncCommandBuilder<T> WithExecute(Func<T?, Task> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _executeWithCancellation = null;
        return this;
    }

    public AsyncCommandBuilder<T> WithCancellation(Func<T?, CancellationToken, Task> execute)
    {
        _executeWithCancellation = execute ?? throw new ArgumentNullException(nameof(execute));
        _execute = null;
        return this;
    }

    public AsyncCommandBuilder<T> WithCanExecute(Func<T?, bool> canExecute)
    {
        _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        return this;
    }

    public AsyncRelayCommand<T> Build()
    {
        if (_execute == null && _executeWithCancellation == null)
            throw new InvalidOperationException(
                "Execute action must be provided before building the command."
            );

        if (_executeWithCancellation != null)
            return new AsyncRelayCommand<T>(_executeWithCancellation, _canExecute);

        return new AsyncRelayCommand<T>(_execute!, _canExecute);
    }
}
