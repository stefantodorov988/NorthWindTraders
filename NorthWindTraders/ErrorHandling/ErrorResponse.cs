namespace NorthWindTraders.ErrorHandling;

/// <summary>
/// Standard API error envelope. Safe for JSON serialization; no ASP.NET-specific types on the model.
/// </summary>
public sealed class ErrorResponse
{
    public required string Timestamp { get; init; }
    public required int Status { get; init; }
    public required string ErrorCode { get; init; }
    public required string Message { get; init; }
    public required string TraceId { get; init; }

    /// <summary>
    /// Optional field-level messages (e.g. validation). Omitted from JSON when null.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
