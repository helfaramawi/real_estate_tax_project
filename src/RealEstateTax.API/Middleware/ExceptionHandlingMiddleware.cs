using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace RealEstateTax.API.Middleware;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errorCode, message) = ex switch
        {
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "FORBIDDEN", "Access denied."),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", ex.Message),
            ArgumentException or InvalidOperationException => (HttpStatusCode.BadRequest, "BAD_REQUEST", ex.Message),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred. Please contact support.")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            success = false,
            errorCode,
            message,
            correlationId = context.TraceIdentifier,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
