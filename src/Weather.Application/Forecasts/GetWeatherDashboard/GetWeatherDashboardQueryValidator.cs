using FluentValidation;
using Weather.Domain.ValueObjects;

namespace Weather.Application.Forecasts.GetWeatherDashboard;

public sealed class GetWeatherDashboardQueryValidator : AbstractValidator<GetWeatherDashboardQuery>
{
    public GetWeatherDashboardQueryValidator()
    {
        RuleFor(query => query.Latitude)
            .InclusiveBetween(Coordinates.MinLatitude, Coordinates.MaxLatitude)
            .WithMessage("Широта должна быть в диапазоне от -90 до 90.");

        RuleFor(query => query.Longitude)
            .InclusiveBetween(Coordinates.MinLongitude, Coordinates.MaxLongitude)
            .WithMessage("Долгота должна быть в диапазоне от -180 до 180.");

        // Провайдер отдаёт почасовой прогноз максимум на 14 дней, экрану достаточно трёх суток из ТЗ
        RuleFor(query => query.ForecastDays)
            .InclusiveBetween(1, 14)
            .WithMessage("Глубина прогноза должна быть от 1 до 14 дней.");
    }
}
