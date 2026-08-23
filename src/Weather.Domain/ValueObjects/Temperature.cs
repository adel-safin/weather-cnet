using System.Globalization;

namespace Weather.Domain.ValueObjects;

/// <summary>
/// Температура в градусах Цельсия. Приложение работает только с метрической
/// системой, поэтому фаренгейты из ответа API сознательно отбрасываются.
/// </summary>
public readonly record struct Temperature(double Celsius)
{
    /// <summary>
    /// Значение для отображения: округление к ближайшему целому,
    /// половинки — от нуля, чтобы -0,5 показывалось как -1, а не как 0.
    /// </summary>
    public int Rounded => (int)Math.Round(Celsius, MidpointRounding.AwayFromZero);

    public string ToDisplayString() =>
        Rounded.ToString("+#;-#;0", CultureInfo.InvariantCulture) + "°";

    public override string ToString() => ToDisplayString();
}
