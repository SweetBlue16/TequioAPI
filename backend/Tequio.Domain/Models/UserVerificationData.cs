namespace Tequio.Domain.Models;

/// <summary>
/// Represents the user data required by verification code workflows.
/// </summary>
/// <param name="UserId">The user identifier.</param>
/// <param name="FirstName">The first name used in email greetings.</param>
/// <param name="IsVerified">Whether the account has already been verified.</param>
public sealed record UserVerificationData(int UserId, string FirstName, bool IsVerified);
