using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos;

/// <summary>
/// Contains an email address for an account-related request.
/// </summary>
public sealed class EmailAddressRequestDto
{
    /// <summary>
    /// Gets or sets the account email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
