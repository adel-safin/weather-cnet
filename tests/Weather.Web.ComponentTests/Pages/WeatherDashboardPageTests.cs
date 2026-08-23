using Bunit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Weather.Application.Configuration;
using Weather.Application.Forecasts.GetWeatherDashboard;
using Weather.Domain.Common;
using Weather.Domain.Forecasts;
using Weather.Web.Components.Pages;
using Weather.Web.ComponentTests.TestSupport;

namespace Weather.Web.ComponentTests.Pages;

public sealed class WeatherDashboardPageTests : BunitContext
{
    private readonly ISender _sender = Substitute.For<ISender>();

    public WeatherDashboardPageTests()
    {
        Services.AddSingleton(_sender);
        Services.AddSingleton(Options.Create(new WeatherLocationOptions
        {
            Name = "Москва",
            Latitude = 55.7558d,
            Longitude = 37.6173d,
        }));
    }

    [Fact]
    public void WhileRequestIsInFlight_ShowsSkeletonInsteadOfEmptyScreen()
    {
        var pending = new TaskCompletionSource<Result<WeatherDashboard>>();
        SendReturns(pending.Task);

        IRenderedComponent<WeatherDashboardPage> page = Render<WeatherDashboardPage>();

        page.Find(".skeleton").ShouldNotBeNull();
        page.Find("button.button--ghost").TextContent.Trim().ShouldBe("Обновляем…");

        pending.SetResult(Result.Success(DashboardData.Dashboard()));
    }

    [Fact]
    public void SuccessfulLoad_RendersCurrentHourlyAndThreeDays()
    {
        SendReturns(Task.FromResult(Result.Success(DashboardData.Dashboard())));

        IRenderedComponent<WeatherDashboardPage> page = Render<WeatherDashboardPage>();

        page.FindAll(".skeleton").ShouldBeEmpty();
        page.Find("h1").TextContent.ShouldBe("Москва");
        page.Find(".current-card__temperature").TextContent.ShouldBe("+21°");

        // Требование ТЗ: остаток текущего дня плюс весь следующий и ровно три дня прогноза.
        page.FindAll(".hour").Count.ShouldBe(26);
        page.FindAll(".day").Count.ShouldBe(3);
    }

    [Fact]
    public void ProviderFailure_ShowsMessageAndRetryButton()
    {
        SendReturns(Task.FromResult(Result.Failure<WeatherDashboard>(WeatherErrors.ProviderUnavailable)));

        IRenderedComponent<WeatherDashboardPage> page = Render<WeatherDashboardPage>();

        page.Find(".error-panel__message").TextContent.ShouldBe(WeatherErrors.ProviderUnavailable.Message);
        page.Find(".error-panel button").TextContent.Trim().ShouldBe("Повторить запрос");
        page.FindAll(".current-card").ShouldBeEmpty();
    }

    [Fact]
    public void RetryAfterFailure_RequestsDataAgainAndRendersDashboard()
    {
        _sender
            .Send(Arg.Any<GetWeatherDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(
                _ => Task.FromResult(Result.Failure<WeatherDashboard>(WeatherErrors.ProviderUnavailable)),
                _ => Task.FromResult(Result.Success(DashboardData.Dashboard())));

        IRenderedComponent<WeatherDashboardPage> page = Render<WeatherDashboardPage>();
        page.Find(".error-panel button").Click();

        page.FindAll(".error-panel").ShouldBeEmpty();
        page.Find(".current-card").ShouldNotBeNull();
        _sender.Received(2).Send(Arg.Any<GetWeatherDashboardQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UnexpectedException_IsShownAsErrorInsteadOfCrashingThePage()
    {
        _sender
            .Send(Arg.Any<GetWeatherDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns<Task<Result<WeatherDashboard>>>(_ => throw new InvalidOperationException("boom"));

        IRenderedComponent<WeatherDashboardPage> page = Render<WeatherDashboardPage>();

        page.Find(".error-panel__message").TextContent
            .ShouldBe("Непредвиденная ошибка при обращении к погодному сервису.");
    }

    [Fact]
    public void Page_QueriesConfiguredLocation()
    {
        SendReturns(Task.FromResult(Result.Success(DashboardData.Dashboard())));

        Render<WeatherDashboardPage>();

        _sender.Received(1).Send(
            Arg.Is<GetWeatherDashboardQuery>(query =>
                query.Latitude == 55.7558d && query.Longitude == 37.6173d && query.ForecastDays == 3),
            Arg.Any<CancellationToken>());
    }

    private void SendReturns(Task<Result<WeatherDashboard>> result) =>
        _sender
            .Send(Arg.Any<GetWeatherDashboardQuery>(), Arg.Any<CancellationToken>())
            .Returns(result);
}
