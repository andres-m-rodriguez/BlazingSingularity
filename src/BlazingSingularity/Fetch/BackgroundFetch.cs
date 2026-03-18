namespace BlazingSingularity.Fetch;

public class BackgroundFetch<T> : IBackgroundFetch<T>, IDisposable
{
    private readonly Func<CancellationToken, Task<T>> _fetchFunc;
    private readonly Func<Task>? _notifyStateChanged;
    private readonly List<Action<IFetch<T>>> _callbacks = [];
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    private FetchStatus _status = FetchStatus.Idle;
    private T? _freshData;
    private T? _staleData;
    private Exception? _error;
    private DateTime? _lastFetchedAt;

    public BackgroundFetch(Func<CancellationToken, Task<T>> fetchFunc, Func<Task>? notifyStateChanged = null)
    {
        _fetchFunc = fetchFunc ?? throw new ArgumentNullException(nameof(fetchFunc));
        _notifyStateChanged = notifyStateChanged;
    }

    public FetchStatus Status => _status;
    public T? Data => _freshData ?? _staleData;
    public T? StaleData => _staleData;
    public Exception? Error => _error;
    public DateTime? LastFetchedAt => _lastFetchedAt;
    public TimeSpan? StaleTime { get; set; }

    public bool IsIdle => _status == FetchStatus.Idle;
    public bool IsLoading => _status == FetchStatus.Loading;
    public bool IsSuccess => _status == FetchStatus.Success;
    public bool IsError => _status == FetchStatus.Error;
    public bool HasData => Data is not null;

    public bool IsStale
    {
        get
        {
            if (_freshData is null && _staleData is not null)
                return true;

            if (StaleTime.HasValue && _lastFetchedAt.HasValue)
                return DateTime.UtcNow - _lastFetchedAt.Value > StaleTime.Value;

            return false;
        }
    }

    public bool IsRefreshing => IsLoading && _staleData is not null;

    public void SetInitialData(T? data)
    {
        _staleData = data;
    }

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

            if (!EqualityComparer<T>.Default.Equals(_freshData, result))
            {
                _freshData = result;
            }

            _error = null;
            _lastFetchedAt = DateTime.UtcNow;
            await SetStatusAsync(FetchStatus.Success);
        }
        catch (OperationCanceledException)
        {
            // Cancelled - don't update status
        }
        catch (Exception ex)
        {
            _error = ex;
            // Keep stale data available on error
            await SetStatusAsync(FetchStatus.Error);
        }
    }

    public async Task RefetchAsync(CancellationToken ct = default)
    {
        // Move fresh data to stale before refetching
        if (_freshData is not null)
        {
            _staleData = _freshData;
            _freshData = default;
        }

        await FetchAsync(ct);
    }

    public void Reset()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;

        _status = FetchStatus.Idle;
        _freshData = default;
        _staleData = default;
        _error = null;
        _lastFetchedAt = null;

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
