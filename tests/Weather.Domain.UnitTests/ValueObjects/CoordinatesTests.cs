using System.Globalization;
using Weather.Domain.Common;
using Weather.Domain.ValueObjects;

namespace Weather.Domain.UnitTests.ValueObjects;

public sealed class CoordinatesTests
{
    [Theory]
    [InlineData(55.7558, 37.6173)]
    [InlineData(0d, 0d)]
    [InlineData(90d, 180d)]
    [InlineData(-90d, -180d)]
    public void Create_ValidValues_ReturnsSuccess(double latitude, double longitude)
    {
        Result<Coordinates> result = Coordinates.Create(latitude, longitude);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Latitude.ShouldBe(latitude);
        result.Value.Longitude.ShouldBe(longitude);
    }

    [Theory]
    [InlineData(90.1d, 0d, "coordinates.latitude")]
    [InlineData(-90.1d, 0d, "coordinates.latitude")]
    [InlineData(double.NaN, 0d, "coordinates.latitude")]
    [InlineData(0d, 180.1d, "coordinates.longitude")]
    [InlineData(0d, -180.1d, "coordinates.longitude")]
    [InlineData(0d, double.NaN, "coordinates.longitude")]
    public void Create_OutOfRange_ReturnsValidationError(double latitude, double longitude, string expectedCode)
    {
        Result<Coordinates> result = Coordinates.Create(latitude, longitude);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(expectedCode);
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    /// <summary>
    /// Регрессионный тест: под русской локалью разделителем дробной части
    /// является запятая, и наивное форматирование превратило бы
    /// "55.7558,37.6173" в "55,7558,37,6173" — внешний API такой запрос отвергнет.
    /// </summary>
    [Fact]
    public void ToQueryValue_UnderRussianCulture_UsesInvariantSeparator()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

            string query = Coordinates.Create(55.7558d, 37.6173d).Value.ToQueryValue();

            query.ShouldBe("55.7558,37.6173");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        Coordinates first = Coordinates.Create(55.75d, 37.61d).Value;
        Coordinates second = Coordinates.Create(55.75d, 37.61d).Value;

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }
}
