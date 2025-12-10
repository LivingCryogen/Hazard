using AzProxy.Requests;
using Microsoft.Extensions.Logging;

namespace AzProxy.Middleware;

public class RequestValidator
{
    private readonly RequestDelegate _next;
    private readonly RequestHandler _requestHandler;
    private readonly ILogger<RequestValidator> _logger;

    public RequestValidator(RequestDelegate next, RequestHandler requestHandler, ILogger<RequestValidator> logger)
    {
        _next = next;
        _requestHandler = requestHandler;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated ?? false
            && context.User.IsInRole("Admin"))
        {
            // Admins bypass further request validation
            await _next(context);
            return;
        }

        // get client IP
        string? clientIP = context.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(clientIP))
        {
            _logger.LogWarning("Request received without IP address");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Invalid request.");
            return;
        }

        // get Request Type
        var requestType = ParseRequestType(context.Request.Path);

        // reject if banned
        if (!await _requestHandler.ValidateRequest(clientIP, requestType))
        {
            _logger.LogInformation("Request from address {clientIP} was rejected due to ban or rate restriction.", clientIP);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Request denied. This has been rate restricted or banned.");
            return;
        }

        _logger.LogInformation("{requesttype} Request from address {clientIP} was accepted.", requestType, clientIP);
        await _next(context);
    }

    private static RequestType ParseRequestType(PathString requestPath)
    {
        return requestPath switch
        {
            _ when requestPath == "/" => RequestType.Verify,
            _ when requestPath.StartsWithSegments("/secure-link") => RequestType.GenSAS,
            _ when requestPath.StartsWithSegments("/sync-stats") => RequestType.Sync,
            _ => RequestType.None,
        };
    }
}
