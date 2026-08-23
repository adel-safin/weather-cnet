namespace Weather.Infrastructure.UnitTests.TestSupport;

/// <summary>Ответы weatherapi.com, сохранённые с боевого эндпоинта: тесты разбирают настоящий JSON провайдера, а не его упрощённую реконструкцию</summary>
internal static class Fixture
{
    public static string CurrentMoscow => Read("current-moscow.json");

    public static string ForecastMoscow => Read("forecast-moscow.json");

    public static string InvalidKeyError => Read("error-invalid-key.json");

    public static string LocationNotFoundError => Read("error-location-not-found.json");

    private static string Read(string fileName) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName));
}
