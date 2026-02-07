namespace BlazingSingularity.Commands;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// </summary>
public readonly struct Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Exception? Exception { get; }
    public string? ErrorMessage => Exception?.Message;

    private Result(bool isSuccess, Exception? exception)
    {
        IsSuccess = isSuccess;
        Exception = exception;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Exception exception) => new(false, exception);

    public void Match(Action onSuccess, Action<Exception> onFailure)
    {
        if (IsSuccess)
            onSuccess();
        else
            onFailure(Exception!);
    }

    public T Match<T>(Func<T> onSuccess, Func<Exception, T> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Exception!);
}

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public Exception? Exception { get; }
    public string? ErrorMessage => Exception?.Message;

    private Result(bool isSuccess, T? value, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Exception = exception;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Exception exception) => new(false, default, exception);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Exception!);

    public void Match(Action<T> onSuccess, Action<Exception> onFailure)
    {
        if (IsSuccess)
            onSuccess(Value!);
        else
            onFailure(Exception!);
    }
}
