namespace Tequio.Domain.Services;

/// <summary>Rate limit for protected authorization code generation.</summary>
public sealed class VerificationCodeRateLimitException(string message, int retryAfterSeconds) : Exception(message)
{
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
