using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Weather.Application.Configuration;
using Weather.Application.Forecasts.GetWeatherDashboard;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Web.Api.Contracts;

namespace Weather.Web.Api;

internal static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RouteGroupBuilder group = builder
            .MapGroup("/api/weather")
            .WithTags("Weather");

        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetWeatherDashboard")
            .WithSummary("Погода для зафиксированной локации")
            .WithDescription(
                "Возвращает текущую погоду, почасовой прогноз (оставшиеся часы сегодня и все часы завтра) " +
                "и прогноз на 3 дня. Локация задана конфигурацией и не принимается от клиента.")
            .Produces<WeatherDashboardResponse>()
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status502BadGateway)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return builder;
    }

    /// <summary>
    /// Координаты берутся из конфигурации, а не из запроса: по ТЗ геолокация
    /// зафиксирована, и открывать её параметром означало бы дать клиенту
    /// возможность гонять чужой платный ключ по произвольным точкам мира.
    /// </summary>
    private static async Task<IResult> GetDashboardAsync(
        ISender sender,
        IOptions<WeatherLocationOptions> locationOptions,
        CancellationToken cancellationToken)
    {
        WeatherLocationOptions location = locationOptions.Value;

        Result<WeatherDashboard> result = await sender.Send(
            new GetWeatherDashboardQuery(location.Latitude, location.Longitude),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value.ToResponse())
            : result.Error.ToProblem();
    }
}
