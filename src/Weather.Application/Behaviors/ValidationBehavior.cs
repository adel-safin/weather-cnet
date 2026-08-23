using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Weather.Application.Behaviors;

/// <summary>
/// Прогоняет все зарегистрированные валидаторы запроса до обработчика.
/// Невалидный запрос — это дефект вызывающей стороны, а не штатный исход
/// сценария, поэтому здесь исключение уместно: внешний слой превратит его
/// в 400 с ProblemDetails.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        IValidator<TRequest>[] applicable = validators.ToArray();
        if (applicable.Length == 0)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        ValidationResult[] results = await Task.WhenAll(
            applicable.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        ValidationFailure[] failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
