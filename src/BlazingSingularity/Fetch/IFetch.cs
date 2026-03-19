namespace BlazingSingularity.Fetch;

public interface IFetch<T> : IDisposable
{
    FetchStatus Status { get; }
    T? Data { get; }
    Exception? Error { get; }

    bool IsIdle { get; }
    bool IsLoading { get; }
    bool IsSuccess { get; }
    bool IsError { get; }
    bool HasData { get; }

    Task FetchAsync(CancellationToken ct = default);
    Task RefetchAsync(CancellationToken ct = default);
    void Reset();

    void OnChange(Action callback);
    void OnChange(Action<IFetch<T>> callback);
}
