using System.Net;
using System.Text.Json;
using FluentValidation;

namespace NoobGg.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleExceptionAsync(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, HttpStatusCode.InternalServerError, "An unexpected error occurred");
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "ValidationError",
            title = "One or more validation errors occurred",
            status = 400,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static async Task HandleExceptionAsync(HttpContext context, HttpStatusCode statusCode, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            type = "Error",
            title = message,
            status = (int)statusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
