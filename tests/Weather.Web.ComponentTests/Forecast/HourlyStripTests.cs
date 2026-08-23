using AngleSharp.Dom;
using Bunit;
using Shouldly;
using Weather.Domain.Forecasts;
using Weather.Web.Api;
using Weather.Web.Api.Contracts;
using Weather.Web.Components.Forecast;
using Weather.Web.ComponentTests.TestSupport;

namespace Weather.Web.ComponentTests.Forecast;

public sealed class HourlyStripTests : BunitContext
{
    [Fact]
    public void Hours_AreGroupedByDayWithHumanReadableTitles()
    {
        IRenderedComponent<HourlyStrip> strip = RenderStrip();

        IReadOnlyList<string> titles = [.. strip.FindAll(".hourly__group-title").Select(node => node.TextContent)];

        titles.ShouldBe(["Сегодня", "Завтра"]);
    }

    [Fact]
    public void CurrentHour_IsHighlightedAndLabelled()
    {
        IRenderedComponent<HourlyStrip> strip = RenderStrip();

        IElement current = strip.Find(".hour--now");

        current.QuerySelector(".hour__time")!.TextContent.ShouldBe("Сейчас");
        strip.FindAll(".hour--now").Count.ShouldBe(1);
    }

    [Fact]
    public void Hours_ShowLocalTimeInTwentyFourHourFormat()
    {
        IRenderedComponent<HourlyStrip> strip = RenderStrip();

        IReadOnlyList<string> times = [.. strip.FindAll(".hour__time").Select(node => node.TextContent)];

        // Первый час - текущий, дальше идут 23:00 и полночь следующего дня
        times[0].ShouldBe("Сейчас");
        times[1].ShouldBe("23:00");
        times[2].ShouldBe("00:00");
    }

    [Fact]
    public void HighChanceOfPrecipitation_IsMarkedForTheUser()
    {
        IRenderedComponent<HourlyStrip> strip = RenderStrip();

        strip.FindAll(".hour__precipitation--likely").Count.ShouldBe(13);
    }

    private IRenderedComponent<HourlyStrip> RenderStrip()
    {
        WeatherDashboardResponse dashboard = DashboardData.Dashboard().ToResponse();

        return Render<HourlyStrip>(parameters => parameters
            .Add(strip => strip.Hours, dashboard.Hourly)
            .Add(strip => strip.LocalNow, dashboard.Location.LocalTime));
    }
}
