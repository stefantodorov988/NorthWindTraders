namespace NorthWindTraders.ErrorHandling;

/// <summary>
/// Typed application/API exception mapped to HTTP status and a stable <see cref="ErrorCode"/>.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiException(int statusCode, string errorCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string ErrorCode { get; }
}
