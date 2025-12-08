using AzProxy.Requests;

namespace AzProxy.Middleware;

public class RequestValidator
{
    private readonly RequestDelegate _next;
    private readonly RequestHandler _requestHandler;

    public RequestValidator(RequestDelegate next, RequestHandler requestHandler)
    {
        _next = next;
        _requestHandler = requestHandler;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated ?? false
            && context.User.IsInRole("Admin"))
        {
            // Admins bypass validation
            await _next(context);
            return;
        }

        
    }
}
