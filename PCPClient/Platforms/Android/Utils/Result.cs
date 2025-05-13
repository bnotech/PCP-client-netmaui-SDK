using System;

namespace Com.Payone.PcpClientSdk.Android
{
    // Simple Result<T> to carry success or error
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public Exception Error { get; }

        private Result(T value)
        {
            IsSuccess = true;
            Value = value;
        }

        private Result(Exception error)
        {
            IsSuccess = false;
            Error = error;
        }

        public static Result<T> Success(T value) => new Result<T>(value);
        public static Result<T> Failure(Exception error) => new Result<T>(error);
    }
}