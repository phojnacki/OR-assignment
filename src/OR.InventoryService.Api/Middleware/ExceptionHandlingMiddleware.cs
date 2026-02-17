using System.Diagnostics;
using System.Net;

namespace OR.InventoryService.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error on {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage });
            await context.Response.WriteAsJsonAsync(new { errors, traceId = GetTraceId(context) });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request on {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = ex.Message, traceId = GetTraceId(context) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "An unexpected error occurred. Please try again later.",
                traceId = GetTraceId(context)
            });
        }
    }

    private static string GetTraceId(HttpContext context)
        => Activity.Current?.Id ?? context.TraceIdentifier;
}
