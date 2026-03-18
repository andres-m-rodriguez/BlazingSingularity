namespace BlazingSingularity.Fetch;

public class Fetch<T> : IFetch<T>, IDisposable
{
    private readonly Func<CancellationToken, Task<T>> _fetchFunc;
    private readonly Func<Task>? _notifyStateChanged;
    private readonly List<Action<IFetch<T>>> _callbacks = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    private FetchStatus _status = FetchStatus.Idle;
    private T? _data;
    private Exception? _error;

    public Fetch(Func<CancellationToken, Task<T>> fetchFunc, Func<Task>? notifyStateChanged = null)
    {
        _fetchFunc = fetchFunc ?? throw new ArgumentNullException(nameof(fetchFunc));
        _notifyStateChanged = notifyStateChanged;
    }

    public FetchStatus Status => _status;
    public T? Data => _data;
    public Exception? Error => _error;

    public bool IsIdle => _status == FetchStatus.Idle;
    public bool IsLoading => _status == FetchStatus.Loading;
    public bool IsSuccess => _status == FetchStatus.Success;
    public bool IsError => _status == FetchStatus.Error;
    public bool HasData => _data is not null;

    public async Task FetchAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await SetStatusAsync(FetchStatus.Loading);

        try
        {
            var result = await _fetchFunc(_cancellationTokenSource.Token);

            if (_cancellationTokenSource.Token.IsCancellationRequested)
                return;

            if (!EqualityComparer<T>.Default.Equals(_data, result))
            {
                _data = result;
            }

            _error = null;
            await SetStatusAsync(FetchStatus.Success);
        }
        catch (OperationCanceledException)
        {
            // Cancelled - don't update status
        }
        catch (Exception ex)
        {
            _error = ex;
            await SetStatusAsync(FetchStatus.Error);
        }
    }

    public async Task RefetchAsync(CancellationToken ct = default)
    {
        await FetchAsync(ct);
    }

    public void Reset()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _status = FetchStatus.Idle;
        _data = default;
        _error = null;

        NotifyCallbacks();
    }

    public void OnChange(Action callback) => _callbacks.Add(_ => callback());

    public void OnChange(Action<IFetch<T>> callback) => _callbacks.Add(callback);

    private async Task SetStatusAsync(FetchStatus status)
    {
        _status = status;
        NotifyCallbacks();

        if (_notifyStateChanged != null)
        {
            await _notifyStateChanged();
        }
    }

    private void NotifyCallbacks()
    {
        foreach (var callback in _callbacks)
            callback(this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
        _callbacks.Clear();
    }
}
