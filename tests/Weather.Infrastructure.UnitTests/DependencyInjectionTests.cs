using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Weather.Application.Abstractions;
using Weather.Infrastructure.WeatherApi;

namespace Weather.Infrastructure.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ExposesProviderPortWithoutLeakingHttpClient()
    {
        using ServiceProvider services = Build(new Dictionary<string, string?>
        {
            ["Weather:WeatherApi:ApiKey"] = "test-key",
        });

        IWeatherProvider provider = services.GetRequiredService<IWeatherProvider>();

        // Наружу выдаётся кэширующий декоратор, а не сам HTTP-клиент.
        provider.ShouldNotBeOfType<WeatherApiClient>();
        provider.GetType().Name.ShouldBe("CachedWeatherProvider");
    }

    [Fact]
    public void AddInfrastructure_MissingApiKey_FailsValidationInsteadOfSilentlyStarting()
    {
        using ServiceProvider services = Build([]);

        Should.Throw<OptionsValidationException>(
            () => services.GetRequiredService<IOptions<WeatherApiOptions>>().Value);
    }

    [Fact]
    public void AddInfrastructure_DefaultBaseAddress_UsesTls()
    {
        using ServiceProvider services = Build(new Dictionary<string, string?>
        {
            ["Weather:WeatherApi:ApiKey"] = "test-key",
        });

        WeatherApiOptions options = services.GetRequiredService<IOptions<WeatherApiOptions>>().Value;

        // В ТЗ адрес указан по http, ключ в открытом канале передавать нельзя.
        options.BaseAddress.Scheme.ShouldBe("https");
        options.Language.ShouldBe("ru");
    }

    [Fact]
    public void AddInfrastructure_CacheDisabled_StillBuildsContainer()
    {
        using ServiceProvider services = Build(new Dictionary<string, string?>
        {
            ["Weather:WeatherApi:ApiKey"] = "test-key",
            ["Weather:WeatherApi:CurrentCacheSeconds"] = "0",
            ["Weather:WeatherApi:ForecastCacheSeconds"] = "0",
        });

        Should.NotThrow(() => services.GetRequiredService<IWeatherProvider>());
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection()
            .AddLogging()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
