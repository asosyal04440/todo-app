namespace TodoApi.Application.Common.Results;

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        IsSuccess = true;
    }

    private Result(AppError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public AppError? Error { get; }
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(AppError error) => new(error);
}
