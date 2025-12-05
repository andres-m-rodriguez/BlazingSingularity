namespace BlazingSingularity.Commands;

public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync(object? parameter);
    bool IsLoading { get; }
    bool IsCancellable { get; }
    event EventHandler? IsLoadingChanged;
    void CancelCommand();
    void RaiseCanExecuteChanged();
}
