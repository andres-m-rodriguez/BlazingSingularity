namespace BlazingSingularity.Commands;

public interface IAsyncCommand
{
    Task ExecuteAsync(object? parameter);
    Task<Result> TryExecuteAsync(object? parameter);
    bool CanExecute(object? parameter);
    bool IsLoading { get; }
    bool IsCancellable { get; }
    event EventHandler? CanExecuteChanged;
    void CancelCommand();
    void RaiseCanExecuteChanged();
}

public interface IAsyncCommand<T> : IAsyncCommand
{
    Task ExecuteAsync(T? parameter);
    Task<Result> TryExecuteAsync(T? parameter);
    bool CanExecute(T? parameter);
}
