using Weather.Domain.ValueObjects;

namespace Weather.Domain.UnitTests.ValueObjects;

public sealed class TemperatureTests
{
    [Theory]
    [InlineData(19.4d, 19)]
    [InlineData(19.5d, 20)]
    [InlineData(-0.4d, 0)]
    [InlineData(-0.5d, -1)]
    [InlineData(-3.6d, -4)]
    public void Rounded_RoundsHalvesAwayFromZero(double celsius, int expected) =>
        new Temperature(celsius).Rounded.ShouldBe(expected);

    [Theory]
    [InlineData(19.2d, "+19°")]
    [InlineData(0.2d, "0°")]
    [InlineData(-7.8d, "-8°")]
    public void ToDisplayString_ShowsSignExceptForZero(double celsius, string expected) =>
        new Temperature(celsius).ToDisplayString().ShouldBe(expected);
}
