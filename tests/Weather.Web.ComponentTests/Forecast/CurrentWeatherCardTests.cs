using Bunit;
using Shouldly;
using Weather.Web.Api;
using Weather.Web.Api.Contracts;
using Weather.Web.Components.Forecast;
using Weather.Web.ComponentTests.TestSupport;

namespace Weather.Web.ComponentTests.Forecast;

public sealed class CurrentWeatherCardTests : BunitContext
{
    [Fact]
    public void Card_ShowsTemperatureConditionAndFeelsLike()
    {
        IRenderedComponent<CurrentWeatherCard> card = RenderCard();

        card.Find(".current-card__temperature").TextContent.ShouldBe("+21°");
        card.Find(".current-card__condition").TextContent.ShouldBe("Солнечно");
        card.Find(".current-card__feels").TextContent.ShouldBe("Ощущается как +16°");
    }

    [Fact]
    public void PressureIsConvertedToMillimetersOfMercury()
    {
        IRenderedComponent<CurrentWeatherCard> card = RenderCard();

        // 1009 гПа - это 757 мм рт. ст.; миллибары российскому пользователю ни о чём не говорят
        card.Markup.ShouldContain("757 мм рт. ст.");
    }

    [Fact]
    public void IconComesFromProviderOverHttps()
    {
        IRenderedComponent<CurrentWeatherCard> card = RenderCard();

        card.Find(".current-card__icon").GetAttribute("src")
            .ShouldStartWith("https://cdn.weatherapi.com/");
    }

    private IRenderedComponent<CurrentWeatherCard> RenderCard()
    {
        WeatherDashboardResponse dashboard = DashboardData.Dashboard().ToResponse();

        return Render<CurrentWeatherCard>(parameters => parameters
            .Add(card => card.Current, dashboard.Current));
    }
}
