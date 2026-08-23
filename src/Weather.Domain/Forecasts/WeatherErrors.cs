using Weather.Domain.Common;

namespace Weather.Domain.Forecasts;

/// <summary>Каталог ошибок погодного сценария - централизованный список кодов избавляет от магических строк в обработчиках и упрощает тесты</summary>
public static class WeatherErrors
{
    public static readonly Error InvalidApiKey = Error.Unauthorized(
        "weather.invalid_api_key",
        "Погодный сервис отклонил ключ доступа. Обратитесь к администратору.");

    public static readonly Error LocationNotFound = Error.NotFound(
        "weather.location_not_found",
        "Погодный сервис не нашёл указанную локацию.");

    public static readonly Error RateLimited = Error.RateLimited(
        "weather.rate_limited",
        "Превышен лимит обращений к погодному сервису. Попробуйте через минуту.");

    public static readonly Error ProviderUnavailable = Error.Unavailable(
        "weather.provider_unavailable",
        "Погодный сервис временно недоступен. Попробуйте повторить запрос.");

    public static readonly Error InvalidResponse = Error.Unexpected(
        "weather.invalid_response",
        "Погодный сервис вернул ответ в неожиданном формате.");

    public static Error BadRequest(string message) => Error.Validation("weather.bad_request", message);
}
