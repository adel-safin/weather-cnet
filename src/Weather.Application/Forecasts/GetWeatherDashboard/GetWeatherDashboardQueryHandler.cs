using MediatR;
using Microsoft.Extensions.Logging;
using Weather.Application.Abstractions;
using Weather.Application.Logging;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Domain.ValueObjects;

namespace Weather.Application.Forecasts.GetWeatherDashboard;

public sealed class GetWeatherDashboardQueryHandler(
    IWeatherProvider weatherProvider,
    TimeProvider timeProvider,
    ILogger<GetWeatherDashboardQueryHandler> logger)
    : IRequestHandler<GetWeatherDashboardQuery, Result<WeatherDashboard>>
{
    public async Task<Result<WeatherDashboard>> Handle(
        GetWeatherDashboardQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        Result<Coordinates> coordinates = Coordinates.Create(request.Latitude, request.Longitude);
        if (coordinates.IsFailure)
        {
            return Result.Failure<WeatherDashboard>(coordinates.Error);
        }

        // ТЗ требует обоих эндпоинтов провайдера - запрашиваем их параллельно: последовательные вызовы удвоили бы время отклика экрана без всякой пользы
        Task<Result<CurrentWeatherSnapshot>> currentTask =
            weatherProvider.GetCurrentWeatherAsync(coordinates.Value, cancellationToken);
        Task<Result<ForecastSnapshot>> forecastTask =
            weatherProvider.GetForecastAsync(coordinates.Value, request.ForecastDays, cancellationToken);

        await Task.WhenAll(currentTask, forecastTask).ConfigureAwait(false);

        Result<CurrentWeatherSnapshot> current = await currentTask.ConfigureAwait(false);
        Result<ForecastSnapshot> forecast = await forecastTask.ConfigureAwait(false);

        // Без прогноза экран собрать нечем, поэтому его ошибка - фатальная
        if (forecast.IsFailure)
        {
            ApplicationLog.ForecastUnavailable(logger, coordinates.Value.ToQueryValue(), forecast.Error.Code);
            return Result.Failure<WeatherDashboard>(forecast.Error);
        }

        ForecastSnapshot forecastSnapshot = forecast.Value;

        // Ответ forecast.json содержит и текущую погоду, поэтому падение отдельного эндпоинта current деградирует мягко, а не роняет экран
        CurrentWeather currentWeather;
        WeatherLocation location;
        if (current.IsSuccess)
        {
            currentWeather = current.Value.Current;
            location = current.Value.Location;
        }
        else
        {
            ApplicationLog.CurrentWeatherDegraded(logger, current.Error.Code);

            currentWeather = forecastSnapshot.Current;
            location = forecastSnapshot.Location;
        }

        DateTimeOffset localNow = timeProvider.GetUtcNow().ToOffset(location.LocalTime.Offset);
        IReadOnlyList<HourlyForecast> hours = HourlyWindow.Select(forecastSnapshot.Hours, localNow);

        ApplicationLog.DashboardComposed(logger, location.Name, hours.Count, forecastSnapshot.Days.Count);

        return Result.Success(new WeatherDashboard(
            location,
            currentWeather,
            hours,
            forecastSnapshot.Days));
    }
}
