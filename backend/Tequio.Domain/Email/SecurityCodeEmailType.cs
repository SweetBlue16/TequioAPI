namespace Tequio.Domain.Email;

/// <summary>
/// Identifies the user-facing purpose of a security code email.
/// </summary>
public enum SecurityCodeEmailType
{
    /// <summary>
    /// Verifies a newly created user account.
    /// </summary>
    AccountVerification,

    /// <summary>
    /// Authorizes a password recovery operation.
    /// </summary>
    PasswordRecovery,

    /// <summary>
    /// Authorizes an administrator registration or protected action.
    /// </summary>
    AdminAuthorization,

    /// <summary>
    /// Completes a second authentication factor.
    /// </summary>
    TwoFactorAuthentication
}
