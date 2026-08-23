namespace Weather.Domain.Common;

/// <summary>Категория ошибки - нужна внешним слоям, чтобы выбрать HTTP-статус и текст для пользователя, не разбирая коды строками</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Unauthorized,
    RateLimited,
    Unavailable,
    Unexpected,
}

/// <summary>Ожидаемая ошибка бизнес-сценария - исключения остаются для того, что действительно является дефектом, а не штатным исходом</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error RateLimited(string code, string message) => new(code, message, ErrorType.RateLimited);

    public static Error Unavailable(string code, string message) => new(code, message, ErrorType.Unavailable);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
