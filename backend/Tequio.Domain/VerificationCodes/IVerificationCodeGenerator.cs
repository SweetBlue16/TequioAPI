namespace Tequio.Domain.VerificationCodes;

/// <summary>
/// Generates security codes for account and authentication workflows.
/// </summary>
public interface IVerificationCodeGenerator
{
    /// <summary>
    /// Generates a six-digit security code that can contain leading zeroes.
    /// </summary>
    /// <returns>A six-character numeric code.</returns>
    string GenerateCode();
}
