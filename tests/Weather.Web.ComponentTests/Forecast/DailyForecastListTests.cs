using AngleSharp.Dom;
using Bunit;
using Shouldly;
using Weather.Web.Api;
using Weather.Web.Api.Contracts;
using Weather.Web.Components.Forecast;
using Weather.Web.ComponentTests.TestSupport;

namespace Weather.Web.ComponentTests.Forecast;

public sealed class DailyForecastListTests : BunitContext
{
    [Fact]
    public void ThreeDays_AreRenderedWithRelativeTitles()
    {
        IRenderedComponent<DailyForecastList> list = RenderList();

        IReadOnlyList<string> titles = [.. list.FindAll(".day__title").Select(node => node.TextContent)];

        titles.Count.ShouldBe(3);
        titles[0].ShouldBe("Сегодня");
        titles[1].ShouldBe("Завтра");
        titles[2].ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Temperatures_ShowMaximumAndMinimum()
    {
        IRenderedComponent<DailyForecastList> list = RenderList();

        IElement firstDay = list.FindAll(".day")[0];

        firstDay.QuerySelector(".day__max")!.TextContent.ShouldBe("+23°");
        firstDay.QuerySelector(".day__min")!.TextContent.ShouldBe("+12°");
    }

    [Fact]
    public void SunTimes_AreConvertedFromProviderAmPmFormat()
    {
        IRenderedComponent<DailyForecastList> list = RenderList();

        IElement sun = list.FindAll(".day")[0].QuerySelectorAll("dd")[^1];

        // Провайдер отдаёт "05:17 AM"/"07:47 PM", читателю на русском нужен 24-часовой формат.
        sun.TextContent.ShouldBe("05:17 — 19:47");
    }

    private IRenderedComponent<DailyForecastList> RenderList()
    {
        WeatherDashboardResponse dashboard = DashboardData.Dashboard().ToResponse();

        return Render<DailyForecastList>(parameters => parameters
            .Add(list => list.Days, dashboard.Daily)
            .Add(list => list.Today, DateOnly.FromDateTime(dashboard.Location.LocalTime.Date)));
    }
}
