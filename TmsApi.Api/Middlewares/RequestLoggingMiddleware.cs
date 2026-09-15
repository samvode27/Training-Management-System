using System.Diagnostics;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Generate a short correlation ID
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        // 2. Set the response header BEFORE calling the next middleware
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // 3. Log entry
        _logger.LogInformation(
            "Request {CorrelationId}: {Method} {Path} started",
            correlationId,
            context.Request.Method,
            context.Request.Path
        );

        // 4. Start timer
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // 5. Call the next middleware (which will eventually hit your endpoint)
            await _next(context);
        }
        finally
        {
            // 6. Log exit (status code, elapsed ms, same correlation ID)
            stopwatch.Stop();
            _logger.LogInformation(
                "Request {CorrelationId}: finished with status {StatusCode} in {ElapsedMs}ms",
                correlationId,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}