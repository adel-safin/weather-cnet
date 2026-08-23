using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Weather.Application.Logging;

namespace Weather.Application.Behaviors;

/// <summary>
/// Сигнализирует о медленных запросах. Порог намеренно невысокий:
/// экран собирается из двух параллельных HTTP-вызовов, и всё, что дольше
/// секунды, означает проблему у провайдера или промах кэша на холодном старте.
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestThresholdMs = 1000;

    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        long startedAt = Stopwatch.GetTimestamp();

        TResponse response = await next(cancellationToken).ConfigureAwait(false);

        TimeSpan elapsed = Stopwatch.GetElapsedTime(startedAt);
        if (elapsed.TotalMilliseconds > SlowRequestThresholdMs)
        {
            ApplicationLog.SlowRequest(logger, RequestName, (long)elapsed.TotalMilliseconds);
        }

        return response;
    }
}
