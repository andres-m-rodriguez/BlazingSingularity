namespace BlazingSingularity.Commands;

public interface IAsyncCommand
{
    Task ExecuteAsync(object? parameter);
    bool CanExecute(object? parameter);
    bool IsLoading { get; }
    bool IsCancellable { get; }
    event EventHandler? CanExecuteChanged;
    void CancelCommand();
    void RaiseCanExecuteChanged();
}
