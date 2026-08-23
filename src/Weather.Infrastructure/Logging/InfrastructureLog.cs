using Microsoft.Extensions.Logging;

namespace Weather.Infrastructure.Logging;

internal static partial class InfrastructureLog
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Debug,
        Message = "Запрос к погодному провайдеру: {Endpoint} для {Coordinates}")]
    public static partial void ProviderRequest(ILogger logger, string endpoint, string coordinates);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Погодный провайдер вернул {StatusCode} для {Endpoint}: код {ProviderCode}, {ProviderMessage}")]
    public static partial void ProviderReturnedError(
        ILogger logger,
        int statusCode,
        string endpoint,
        int providerCode,
        string providerMessage);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "Обращение к погодному провайдеру не удалось: {Endpoint}")]
    public static partial void ProviderCallFailed(ILogger logger, string endpoint, Exception exception);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Погодный провайдер не уложился в таймаут: {Endpoint}")]
    public static partial void ProviderTimedOut(ILogger logger, string endpoint, Exception exception);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Debug,
        Message = "Промах кэша {CacheKey}, идём в провайдер")]
    public static partial void CacheMiss(ILogger logger, string cacheKey);
}
