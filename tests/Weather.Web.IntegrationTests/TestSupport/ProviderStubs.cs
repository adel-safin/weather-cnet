using System.Net;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Weather.Web.IntegrationTests.TestSupport;

internal static class ProviderStubs
{
    public static string CurrentMoscow => ReadFixture("current-moscow.json");

    public static string ForecastMoscow => ReadFixture("forecast-moscow.json");

    public static void StubHappyPath(this WireMockServer server)
    {
        server.StubJson("/v1/current.json", CurrentMoscow);
        server.StubJson("/v1/forecast.json", ForecastMoscow);
    }

    public static void StubJson(
        this WireMockServer server,
        string path,
        string body,
        HttpStatusCode statusCode = HttpStatusCode.OK) =>
        server
            .Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(body));

    private static string ReadFixture(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));
}
