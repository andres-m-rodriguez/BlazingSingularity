namespace BlazingSingularity.Signals;

public class Signal<T>
{
    private readonly List<Action<T>> _callbacks = [];

    public void OnChange(Action<T> callback) => _callbacks.Add(callback);

    public void OnChange(Action callback) => _callbacks.Add(_ => callback());

    internal void Notify(T value)
    {
        foreach (var callback in _callbacks)
            callback(value);
    }
}
