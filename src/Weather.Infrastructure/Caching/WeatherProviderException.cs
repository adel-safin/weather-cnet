using Weather.Domain.Common;

namespace Weather.Infrastructure.Caching;

/// <summary>Транспорт доменной ошибки через границу фабрики <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> - фабрика кэша либо возвращает значение (и оно сохраняется), либо бросает исключение - третьего варианта в её API нет - исключение здесь гарантирует, что неудачный ответ провайдера не залипнет в кэше на весь TTL</summary>
internal sealed class WeatherProviderException : Exception
{
    public WeatherProviderException(Error error)
        : base(error?.Message) => Error = error ?? Domain.Common.Error.None;

    public WeatherProviderException()
        : this(Domain.Common.Error.None)
    {
    }

    public WeatherProviderException(string message)
        : base(message) => Error = Domain.Common.Error.Unexpected("weather.unexpected", message);

    public WeatherProviderException(string message, Exception innerException)
        : base(message, innerException) => Error = Domain.Common.Error.Unexpected("weather.unexpected", message);

    public Error Error { get; }
}
