namespace Weather.Domain.Forecasts;

/// <summary>
/// Погодное явление: текстовое описание, иконка и код провайдера.
/// </summary>
/// <param name="Text">Локализованное описание, например «Солнечно».</param>
/// <param name="IconUrl">Абсолютный URL иконки. Провайдер отдаёт protocol-relative адрес,
/// приведение к абсолютному — забота слоя инфраструктуры.</param>
/// <param name="Code">Числовой код явления из справочника провайдера.</param>
public sealed record WeatherCondition(string Text, Uri IconUrl, int Code);
