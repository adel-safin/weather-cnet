using FluentValidation.Results;
using Weather.Application.Forecasts.GetWeatherDashboard;

namespace Weather.Application.UnitTests.Forecasts;

public sealed class GetWeatherDashboardQueryValidatorTests
{
    private readonly GetWeatherDashboardQueryValidator _validator = new();

    [Fact]
    public void Validate_MoscowCoordinates_IsValid() =>
        _validator.Validate(new GetWeatherDashboardQuery(55.7558d, 37.6173d)).IsValid.ShouldBeTrue();

    [Theory]
    [InlineData(90.5d, 37d, nameof(GetWeatherDashboardQuery.Latitude))]
    [InlineData(55d, 200d, nameof(GetWeatherDashboardQuery.Longitude))]
    public void Validate_CoordinatesOutOfRange_ReportsFailingProperty(
        double latitude,
        double longitude,
        string expectedProperty)
    {
        ValidationResult result = _validator.Validate(new GetWeatherDashboardQuery(latitude, longitude));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(failure => failure.PropertyName == expectedProperty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    public void Validate_ForecastDepthOutOfRange_IsInvalid(int days) =>
        _validator.Validate(new GetWeatherDashboardQuery(55.7558d, 37.6173d, days)).IsValid.ShouldBeFalse();
}
