using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TmsApi.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString("N");
        var stopwatch = Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestName"] = requestName,
            ["CorrelationId"] = correlationId
        });

        _logger.LogInformation("Handling {RequestName} (cid={CorrelationId})", requestName, correlationId);

        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms (cid={CorrelationId})",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed {RequestName} after {ElapsedMs}ms (cid={CorrelationId})",
                requestName, stopwatch.ElapsedMilliseconds, correlationId);
            throw;
        }
    }
}