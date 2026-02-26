namespace DandDTemplateParserCSharp.Domain;

// Two-type-parameter variant — used by domains that have their own error hierarchy.
public sealed class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    private Result(TValue value)  { _value = value; IsSuccess = true; }
    private Result(TError error)  { _error = error; IsSuccess = false; }

    public bool   IsSuccess { get; }
    public bool   IsFailure => !IsSuccess;

    public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is a failure.");
    public TError Error => IsFailure ? _error! : throw new InvalidOperationException("Result is a success.");

    public static Result<TValue, TError> Success(TValue value) => new(value);
    public static Result<TValue, TError> Failure(TError error) => new(error);

    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<TError, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}


// Minimal Result<T> — keeps service signatures honest without a third-party library.
public sealed class Result<T>
{
    private readonly T?         _value;
    private readonly TaskError? _error;

    private Result(T value)          { _value = value; IsSuccess = true; }
    private Result(TaskError error)  { _error = error; IsSuccess = false; }

    public bool      IsSuccess { get; }
    public bool      IsFailure => !IsSuccess;

    public T         Value => IsSuccess ? _value! : throw new InvalidOperationException("Result is a failure.");
    public TaskError Error  => IsFailure ? _error! : throw new InvalidOperationException("Result is a success.");

    public static Result<T> Success(T value)         => new(value);
    public static Result<T> Failure(TaskError error) => new(error);

    // Pattern-match helper
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<TaskError, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);
}
