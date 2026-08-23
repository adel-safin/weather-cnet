namespace Weather.Domain.Common;

/// <summary>
/// Результат операции без возвращаемого значения.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Успешный результат не может содержать ошибку.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Неуспешный результат обязан содержать ошибку.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.FromValue(value);

    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.FromError(error);
}

/// <summary>
/// Результат операции со значением. Обращение к <see cref="Value"/> у неуспешного
/// результата — программная ошибка, поэтому здесь исключение уместно.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Нельзя читать значение неуспешного результата. Ошибка: {Error.Code}.");

    internal static Result<TValue> FromValue(TValue value) => new(value, true, Error.None);

    internal static Result<TValue> FromError(Error error) => new(default, false, error);

    public static implicit operator Result<TValue>(TValue value) => FromValue(value);

    /// <summary>
    /// Преобразует успешное значение, пробрасывая ошибку без изменений.
    /// </summary>
    public Result<TNext> Map<TNext>(Func<TValue, TNext> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return IsSuccess ? Result<TNext>.FromValue(map(Value)) : Result<TNext>.FromError(Error);
    }
}
