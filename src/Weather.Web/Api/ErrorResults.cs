using Microsoft.AspNetCore.Http;
using Weather.Domain.Common;

namespace Weather.Web.Api;

/// <summary>
/// Единое место, где доменная ошибка превращается в HTTP-ответ.
/// Категория ошибки определяет статус, поэтому новый код ошибки
/// не требует правок в эндпоинтах.
/// </summary>
internal static class ErrorResults
{
    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        (int statusCode, string title) = error.Type switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Некорректный запрос"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Данные не найдены"),
            ErrorType.Unauthorized => (StatusCodes.Status502BadGateway, "Ошибка доступа к погодному сервису"),
            ErrorType.RateLimited => (StatusCodes.Status429TooManyRequests, "Слишком много запросов"),
            ErrorType.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Сервис временно недоступен"),
            _ => (StatusCodes.Status502BadGateway, "Ошибка получения погоды"),
        };

        return Results.Problem(
            title: title,
            detail: error.Message,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["errorCode"] = error.Code,
            });
    }
}
