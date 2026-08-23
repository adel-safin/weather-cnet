using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Scalar.AspNetCore;
using Serilog;
using Weather.Application;
using Weather.Infrastructure;
using Weather.Web.Api;
using Weather.Web.Components;
using Weather.Web.Health;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, configuration) => configuration
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// MediatR 13+ распространяется по двойной лицензии и на старте пишет предупреждение
// об отсутствии коммерческого ключа. Для разработки и тестирования ключ не требуется,
// поэтому шум глушим точечно, не трогая остальные логи библиотеки.
builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Кодировщики по умолчанию экранируют всё за пределами ASCII: каждая русская буква
// превращается в "&#x41C;" в HTML и в "\u041C" в JSON, раздувая ответ в несколько раз
// и делая его нечитаемым при отладке.
builder.Services.AddSingleton(HtmlEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic));
builder.Services.ConfigureHttpJsonOptions(json =>
    json.SerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddCheck<WeatherProviderHealthCheck>("weather-provider", tags: ["ready"]);

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();

app.UseExceptionHandler("/Error", createScopeForErrors: true);

// В контейнере за обратным прокси TLS терминируется снаружи: включённый там
// редирект отправлял бы пользователя на несуществующий порт.
bool useHttpsRedirection = app.Configuration.GetValue("Weather:UseHttpsRedirection", defaultValue: true);

if (!app.Environment.IsDevelopment() && useHttpsRedirection)
{
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

if (useHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapWeatherEndpoints();

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Weather API"));
}

await app.RunAsync().ConfigureAwait(false);

/// <summary>
/// Явное объявление точки входа нужно, чтобы интеграционные тесты
/// могли поднять приложение через WebApplicationFactory.
/// </summary>
public partial class Program;
