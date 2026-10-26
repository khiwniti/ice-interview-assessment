using Microsoft.Extensions.Logging;

namespace BusinessAnalytics.Api.Middleware;

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
        var start = DateTimeOffset.UtcNow;
        var traceId = context.TraceIdentifier;
        
        _logger.LogInformation("Request started: {Method} {Path} TraceId: {TraceId}", 
            context.Request.Method, context.Request.Path, traceId);
        
        try
        {
            await _next(context);
        }
        finally
        {
            var elapsed = DateTimeOffset.UtcNow - start;
            _logger.LogInformation("Request completed: {Method} {Path} {StatusCode} in {Elapsed}ms TraceId: {TraceId}", 
                context.Request.Method, context.Request.Path, context.Response.StatusCode, elapsed.TotalMilliseconds, traceId);
        }
    }
}