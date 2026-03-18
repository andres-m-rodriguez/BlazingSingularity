namespace BlazingSingularity.Fetch;

public interface IBackgroundFetch<T> : IFetch<T>
{
    bool IsStale { get; }
    bool IsRefreshing { get; }
    DateTime? LastFetchedAt { get; }
    T? StaleData { get; }

    void SetInitialData(T? data);
    TimeSpan? StaleTime { get; set; }
}
