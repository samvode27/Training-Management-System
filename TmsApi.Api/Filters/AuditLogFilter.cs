using Microsoft.AspNetCore.Mvc.Filters;

namespace TmsApi.Api.Filters;

public class AuditLogFilter : IActionFilter
{
    private readonly ILogger<AuditLogFilter> _logger;

    public AuditLogFilter(ILogger<AuditLogFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var route = context.HttpContext.Request.Path;
        var method = context.HttpContext.Request.Method;
        _logger.LogInformation("TMS API call: {Method} {Route}", method, route);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        var status = context.HttpContext.Response.StatusCode;
        _logger.LogInformation("TMS API response: {StatusCode}", status);
    }
}