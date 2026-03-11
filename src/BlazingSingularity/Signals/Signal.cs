namespace BlazingSingularity.Signals;

public class Signal<T>
{
    private T _value = default!;
    private readonly List<Action<T>> _callbacks = [];

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                Notify(value);
            }
        }
    }

    public void OnChange(Action<T> callback) => _callbacks.Add(callback);

    public void OnChange(Action callback) => _callbacks.Add(_ => callback());

    public void Notify(T value)
    {
        foreach (var callback in _callbacks)
            callback(value);
    }
}
