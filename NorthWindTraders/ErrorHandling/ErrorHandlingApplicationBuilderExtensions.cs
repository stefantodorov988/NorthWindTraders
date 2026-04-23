namespace NorthWindTraders.ErrorHandling;

public static class ErrorHandlingApplicationBuilderExtensions
{
    /// <summary>
    /// Registers global exception handling as the outermost middleware (register immediately after <see cref="WebApplication"/> is built).
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionMiddleware>();
}
