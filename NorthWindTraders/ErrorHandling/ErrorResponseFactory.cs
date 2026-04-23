using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NorthWindTraders.ErrorHandling;

public static class ErrorResponseFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static ErrorResponse FromUnhandled(
        Exception exception,
        bool exposeDetails,
        HttpContext httpContext)
    {
        var traceId = ResolveTraceId(httpContext);
        var message = exposeDetails
            ? exception.Message
            : "An unexpected error occurred. Please try again later.";

        return new ErrorResponse
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Status = StatusCodes.Status500InternalServerError,
            ErrorCode = ErrorCodes.InternalError,
            Message = message,
            TraceId = traceId,
        };
    }

    public static ErrorResponse FromApiException(ApiException exception, HttpContext httpContext) =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Status = exception.StatusCode,
            ErrorCode = exception.ErrorCode,
            Message = exception.Message,
            TraceId = ResolveTraceId(httpContext),
        };

    /// <summary>Reserved for validation pipeline (e.g. filter on ModelState).</summary>
    public static ErrorResponse Validation(
        IReadOnlyDictionary<string, string[]> errors,
        HttpContext httpContext) =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Status = StatusCodes.Status400BadRequest,
            ErrorCode = ErrorCodes.ValidationFailed,
            Message = "One or more validation errors occurred.",
            TraceId = ResolveTraceId(httpContext),
            Errors = errors,
        };

    /// <summary>Reserved for authorization failures (e.g. 403 handler).</summary>
    public static ErrorResponse Forbidden(string message, HttpContext httpContext) =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Status = StatusCodes.Status403Forbidden,
            ErrorCode = ErrorCodes.Forbidden,
            Message = message,
            TraceId = ResolveTraceId(httpContext),
        };

    /// <summary>Reserved for unauthenticated responses (401).</summary>
    public static ErrorResponse Unauthorized(string message, HttpContext httpContext) =>
        new()
        {
            Timestamp = DateTimeOffset.UtcNow.ToString("O"),
            Status = StatusCodes.Status401Unauthorized,
            ErrorCode = ErrorCodes.Unauthorized,
            Message = message,
            TraceId = ResolveTraceId(httpContext),
        };

    public static string ResolveTraceId(HttpContext httpContext) =>
        Activity.Current?.Id ?? httpContext.TraceIdentifier;

    public static Task WriteJsonAsync(HttpContext httpContext, ErrorResponse body, CancellationToken cancellationToken = default)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = body.Status;
        return httpContext.Response.WriteAsJsonAsync(body, JsonOptions, cancellationToken);
    }
}
