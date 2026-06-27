namespace RbacApp.Domain.Common;

/// <summary>
/// نتیجه‌ی یک عملیات بدون پرتاب استثنا.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string errorCode = "failure")
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error, string errorCode = "failure")
        => new(false, error, errorCode);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(string error, string errorCode = "failure")
        => new(default, false, error, errorCode);
}

public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    internal Result(T? value, bool isSuccess, string? error, string errorCode = "failure")
        : base(isSuccess, error, errorCode)
    {
        _value = value;
    }
}
