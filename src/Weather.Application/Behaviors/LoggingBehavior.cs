using MediatR;
using Microsoft.Extensions.Logging;
using Weather.Application.Logging;
using Weather.Domain.Common;

namespace Weather.Application.Behaviors;

/// <summary>Единая точка логирования всех запросов: имя, исход, код ошибки - избавляет обработчики от дублирующего кода логирования</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    // Статическое поле generic-класса вычисляется один раз на каждую специализацию
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        ApplicationLog.RequestHandling(logger, RequestName);

        try
        {
            TResponse response = await next(cancellationToken).ConfigureAwait(false);

            if (response is Result { IsFailure: true } failure)
            {
                ApplicationLog.RequestFailed(logger, RequestName, failure.Error.Code, failure.Error.Message);
            }
            else
            {
                ApplicationLog.RequestSucceeded(logger, RequestName);
            }

            return response;
        }
        catch (Exception exception)
        {
            ApplicationLog.RequestThrew(logger, RequestName, exception);
            throw;
        }
    }
}
