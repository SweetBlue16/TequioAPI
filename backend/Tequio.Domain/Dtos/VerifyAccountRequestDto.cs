using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos;

/// <summary>
/// Contains the data required to verify a user account.
/// </summary>
public sealed class VerifyAccountRequestDto
{
    /// <summary>
    /// Gets or sets the account email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the six-digit verification code.
    /// </summary>
    [Required]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código de verificación debe tener 6 dígitos.")]
    public string VerificationCode { get; set; } = string.Empty;
}
