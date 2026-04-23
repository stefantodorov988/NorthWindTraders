namespace NorthWindTraders.ErrorHandling;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception after response started; trace {TraceId}",
                    ErrorResponseFactory.ResolveTraceId(context));
                throw;
            }

            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        ErrorResponse body;
        if (exception is ApiException apiEx)
        {
            body = ErrorResponseFactory.FromApiException(apiEx, context);
            if (apiEx.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(apiEx, "API exception {ErrorCode}; trace {TraceId}", apiEx.ErrorCode, body.TraceId);
            }
            else
            {
                _logger.LogWarning(apiEx, "API exception {ErrorCode}; trace {TraceId}", apiEx.ErrorCode, body.TraceId);
            }
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception; trace {TraceId}",
                ErrorResponseFactory.ResolveTraceId(context));

            body = ErrorResponseFactory.FromUnhandled(
                exception,
                exposeDetails: _environment.IsDevelopment(),
                context);
        }

        await ErrorResponseFactory.WriteJsonAsync(context, body);
    }
}
