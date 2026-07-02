namespace OnlinePortfolio.Api.Common;

public sealed record Error(string Code, string Message)
{
    public static Error NotFound(string resource) =>
        new($"{resource}.NotFound", $"{resource} not found.");

    public static Error Conflict(string resource) =>
        new($"{resource}.Conflict", $"{resource} already exists.");

    public static Error Unauthorized() =>
        new("Auth.Unauthorized", "Authentication required.");

    public static Error Forbidden() =>
        new("Auth.Forbidden", "You do not have permission to perform this action.");

    public static Error Validation(string message) =>
        new("Validation.Failed", message);
}

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)   { _value = value; IsSuccess = true;  _error = null; }
    private Result(Error err) { _error = err;   IsSuccess = false; _value = default; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess ? _value! : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public Error Error =>
        !IsSuccess ? _error! : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    public static Result<T> Ok(T value)    => new(value);
    public static Result<T> Fail(Error e)  => new(e);
    public static Result<T> Fail(string code, string message) => new(new Error(code, message));

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}

public readonly struct Result
{
    private readonly Error? _error;

    private Result(bool success, Error? error) { IsSuccess = success; _error = error; }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Error Error =>
        !IsSuccess ? _error! : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    public static Result Ok()              => new(true,  null);
    public static Result Fail(Error e)     => new(false, e);
    public static Result Fail(string code, string message) => new(false, new Error(code, message));

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(_error!);
}
