using Tequio.Domain.Email;

namespace Tequio.Domain.Repositories;

/// <summary>
/// Defines persistence operations for purpose-specific verification codes.
/// </summary>
public interface IVerificationCodeRepository
{
    /// <summary>
    /// Invalidates active codes of the same type and persists a new code.
    /// </summary>
    Task SaveAsync(
        int userId,
        string verificationCode,
        SecurityCodeEmailType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and consumes the latest verification code through the database procedure.
    /// </summary>
    Task ValidateAsync(
        int userId,
        string verificationCode,
        SecurityCodeEmailType type,
        CancellationToken cancellationToken = default);
}
