using MediatR;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;

namespace Weather.Application.Forecasts.GetWeatherDashboard;

/// <summary>Запрос всех данных для единственного экрана приложения</summary>
/// <param name="Latitude">Широта точки запроса</param>
/// <param name="Longitude">Долгота точки запроса</param>
/// <param name="ForecastDays">Глубина посуточного прогноза в днях</param>
public sealed record GetWeatherDashboardQuery(double Latitude, double Longitude, int ForecastDays = 3)
    : IRequest<Result<WeatherDashboard>>;
