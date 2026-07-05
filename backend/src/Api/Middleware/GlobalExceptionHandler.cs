using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            ArgumentException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        var code = exception switch
        {
            UnauthorizedAccessException => "FORBIDDEN",
            ArgumentException => "BAD_REQUEST",
            _ => "INTERNAL_SERVER_ERROR"
        };

        var message = exception switch
        {
            UnauthorizedAccessException => exception.Message,
            ArgumentException => exception.Message,
            _ => "An unexpected error occurred."
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";
        
        var correlationId = httpContext.Items["CorrelationId"]?.ToString() 
                            ?? httpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
                            ?? Guid.NewGuid().ToString();

        var response = new
        {
            error = new
            {
                code = code,
                message = message,
                correlationId = correlationId,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        };

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
