using System.Net;
using Shouldly;
using Weather.Web.IntegrationTests.TestSupport;

namespace Weather.Web.IntegrationTests;

/// <summary>
/// Проверки страницы на стороне сервера: интересует предварительный рендер,
/// то есть то, что пользователь видит до подключения интерактивного канала.
/// </summary>
public sealed class WeatherPageTests : IDisposable
{
    private readonly WeatherAppFactory _factory = new();

    [Fact]
    public async Task HomePage_PrerendersWeatherWithoutWaitingForInteractivity()
    {
        _factory.Provider.StubHappyPath();
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/", UriKind.Relative));
        string html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Москва");
        html.ShouldContain("current-card__temperature");
        html.ShouldContain("Солнечно");
    }

    [Fact]
    public async Task HomePage_WhenProviderIsDown_PrerendersErrorWithRetryButton()
    {
        _factory.Provider.StubJson("/v1/current.json", string.Empty, HttpStatusCode.InternalServerError);
        _factory.Provider.StubJson("/v1/forecast.json", string.Empty, HttpStatusCode.InternalServerError);
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/", UriKind.Relative));
        string html = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("Не удалось загрузить погоду");
        html.ShouldContain("Повторить запрос");
    }

    [Fact]
    public async Task UnknownRoute_ReturnsLocalizedNotFoundPage()
    {
        _factory.Provider.StubHappyPath();
        using HttpClient client = _factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri("/no-such-page", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldContain("Страница не найдена");
    }

    public void Dispose() => _factory.Dispose();
}
