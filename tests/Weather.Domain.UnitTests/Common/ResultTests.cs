using Weather.Domain.Common;

namespace Weather.Domain.UnitTests.Common;

public sealed class ResultTests
{
    private static readonly Error SampleError = Error.Unavailable("test.error", "Тестовая ошибка");

    [Fact]
    public void Success_ExposesValue()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Value.ShouldBe(42);
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_ReadingValue_Throws()
    {
        Result<int> result = Result.Failure<int>(SampleError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SampleError);
        Should.Throw<InvalidOperationException>(() => _ = result.Value);
    }

    [Fact]
    public void Map_Success_TransformsValue() =>
        Result.Success(21).Map(value => value * 2).Value.ShouldBe(42);

    [Fact]
    public void Map_Failure_PropagatesErrorWithoutInvokingMapper()
    {
        bool mapperCalled = false;

        Result<string> mapped = Result.Failure<int>(SampleError).Map(value =>
        {
            mapperCalled = true;
            return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        });

        mapped.IsFailure.ShouldBeTrue();
        mapped.Error.ShouldBe(SampleError);
        mapperCalled.ShouldBeFalse();
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "готово";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("готово");
    }
}
