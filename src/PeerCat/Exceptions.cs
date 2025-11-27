using System.Net.Http.Headers;

namespace PeerCat;

/// <summary>
/// Rate limit information from response headers
/// </summary>
public class RateLimitInfo
{
    /// <summary>Maximum requests allowed in the window</summary>
    public int? Limit { get; init; }

    /// <summary>Remaining requests in the current window</summary>
    public int? Remaining { get; init; }

    /// <summary>Unix timestamp when the rate limit resets</summary>
    public long? Reset { get; init; }

    /// <summary>Seconds to wait before retrying (from Retry-After header)</summary>
    public int? RetryAfter { get; init; }

    /// <summary>
    /// Parse rate limit information from HTTP response headers
    /// </summary>
    public static RateLimitInfo? FromHeaders(HttpResponseHeaders headers)
    {
        int? limit = null;
        int? remaining = null;
        long? reset = null;
        int? retryAfter = null;

        if (headers.TryGetValues("X-RateLimit-Limit", out var limitValues))
        {
            if (int.TryParse(limitValues.FirstOrDefault(), out var l))
                limit = l;
        }

        if (headers.TryGetValues("X-RateLimit-Remaining", out var remainingValues))
        {
            if (int.TryParse(remainingValues.FirstOrDefault(), out var r))
                remaining = r;
        }

        if (headers.TryGetValues("X-RateLimit-Reset", out var resetValues))
        {
            if (long.TryParse(resetValues.FirstOrDefault(), out var rs))
                reset = rs;
        }

        if (headers.TryGetValues("Retry-After", out var retryValues))
        {
            if (int.TryParse(retryValues.FirstOrDefault(), out var ra))
                retryAfter = ra;
        }

        if (limit == null && remaining == null && reset == null && retryAfter == null)
            return null;

        return new RateLimitInfo
        {
            Limit = limit,
            Remaining = remaining,
            Reset = reset,
            RetryAfter = retryAfter
        };
    }
}

/// <summary>
/// Base exception for all PeerCat API errors
/// </summary>
public class PeerCatException : Exception
{
    /// <summary>Error type from API</summary>
    public string Type { get; }

    /// <summary>Error code from API</summary>
    public string Code { get; }

    /// <summary>Parameter that caused the error</summary>
    public string? Param { get; }

    /// <summary>HTTP status code</summary>
    public int Status { get; }

    public PeerCatException(string message, string type, string code, string? param = null, int status = 500)
        : base(message)
    {
        Type = type;
        Code = code;
        Param = param;
        Status = status;
    }

    /// <summary>
    /// Returns true if this error is retryable
    /// </summary>
    public virtual bool IsRetryable => Status >= 500 || Status == 429;

    internal static PeerCatException FromApiError(int status, ApiErrorDetail error, RateLimitInfo? rateLimitInfo = null)
    {
        return error.Type switch
        {
            "authentication_error" => new AuthenticationException(error.Message, error.Code, error.Param),
            "invalid_request_error" => new InvalidRequestException(error.Message, error.Code, error.Param),
            "insufficient_credits" => new InsufficientCreditsException(error.Message, error.Code),
            "rate_limit_error" => new RateLimitException(error.Message, error.Code, rateLimitInfo?.RetryAfter, rateLimitInfo),
            "not_found" => new NotFoundException(error.Message, error.Code, error.Param),
            _ => new PeerCatException(error.Message, error.Type, error.Code, error.Param, status)
        };
    }
}

/// <summary>
/// Authentication error (invalid or missing API key)
/// </summary>
public class AuthenticationException : PeerCatException
{
    public AuthenticationException(string message, string code, string? param = null)
        : base(message, "authentication_error", code, param, 401) { }

    public override bool IsRetryable => false;
}

/// <summary>
/// Invalid request error (bad parameters)
/// </summary>
public class InvalidRequestException : PeerCatException
{
    public InvalidRequestException(string message, string code, string? param = null)
        : base(message, "invalid_request_error", code, param, 400) { }

    public override bool IsRetryable => false;
}

/// <summary>
/// Insufficient credits error
/// </summary>
public class InsufficientCreditsException : PeerCatException
{
    public InsufficientCreditsException(string message, string code)
        : base(message, "insufficient_credits", code, null, 402) { }

    public override bool IsRetryable => false;
}

/// <summary>
/// Rate limit error
/// </summary>
public class RateLimitException : PeerCatException
{
    /// <summary>Time to wait before retrying (seconds)</summary>
    public int? RetryAfter { get; init; }

    /// <summary>Full rate limit information from response headers</summary>
    public RateLimitInfo? RateLimitInfo { get; init; }

    public RateLimitException(string message, string code, int? retryAfter = null, RateLimitInfo? rateLimitInfo = null)
        : base(message, "rate_limit_error", code, null, 429)
    {
        RetryAfter = retryAfter;
        RateLimitInfo = rateLimitInfo;
    }

    public override bool IsRetryable => true;
}

/// <summary>
/// Resource not found error
/// </summary>
public class NotFoundException : PeerCatException
{
    public NotFoundException(string message, string code, string? param = null)
        : base(message, "not_found", code, param, 404) { }

    public override bool IsRetryable => false;
}
