using Microsoft.Extensions.Logging;

namespace Weather.Application.Logging;

/// <summary>
/// Логи слоя приложения через source-generated LoggerMessage:
/// нулевые аллокации на отключённом уровне и стабильные EventId для алертов.
/// </summary>
internal static partial class ApplicationLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Обработка запроса {RequestName}")]
    public static partial void RequestHandling(ILogger logger, string requestName);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Запрос {RequestName} обработан успешно")]
    public static partial void RequestSucceeded(ILogger logger, string requestName);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Запрос {RequestName} завершился ошибкой {ErrorCode}: {ErrorMessage}")]
    public static partial void RequestFailed(ILogger logger, string requestName, string errorCode, string errorMessage);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Запрос {RequestName} завершился исключением")]
    public static partial void RequestThrew(ILogger logger, string requestName, Exception exception);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Медленный запрос {RequestName}: {ElapsedMilliseconds} мс")]
    public static partial void SlowRequest(ILogger logger, string requestName, long elapsedMilliseconds);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Warning,
        Message = "Не удалось получить прогноз для {Coordinates}: {ErrorCode}")]
    public static partial void ForecastUnavailable(ILogger logger, string coordinates, string errorCode);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Warning,
        Message = "Эндпоинт текущей погоды недоступен ({ErrorCode}), используем данные из прогноза")]
    public static partial void CurrentWeatherDegraded(ILogger logger, string errorCode);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Information,
        Message = "Собран экран погоды для {Location}: {HourCount} часов, {DayCount} дней")]
    public static partial void DashboardComposed(ILogger logger, string location, int hourCount, int dayCount);
}
