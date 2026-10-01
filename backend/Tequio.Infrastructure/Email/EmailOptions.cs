using System.ComponentModel.DataAnnotations;

namespace Tequio.Infrastructure.Email;

/// <summary>
/// Defines the external SMTP settings used to deliver Tequio emails.
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Gets or sets the SMTP host.
    /// </summary>
    [Required]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP port.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets the SMTP account name supplied through secret configuration.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the SMTP App Password supplied through secret configuration.
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender display name.
    /// </summary>
    [Required]
    public string FromName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the SMTP connection must be upgraded with STARTTLS.
    /// </summary>
    public bool UseStartTls { get; set; }
}
