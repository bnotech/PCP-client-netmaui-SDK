namespace Com.Payone.PcpClientSdk.iOS;

public struct Result<T, E>
{
    public bool IsSuccess { get; }
    public T Value       { get; }
    public E Error       { get; }

    Result(T value)
    {
        IsSuccess = true;
        Value     = value;
        Error     = default;
    }

    Result(E error)
    {
        IsSuccess = false;
        Error     = error;
        Value     = default;
    }

    public static Result<T, E> Success(T v) => new(v);
    public static Result<T, E> Fail(E e)    => new(e);
}