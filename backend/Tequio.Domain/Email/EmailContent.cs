namespace Tequio.Domain.Email;

/// <summary>
/// Represents localized email content independently from its delivery mechanism.
/// </summary>
public sealed class EmailContent
{
    /// <summary>
    /// Gets the email subject.
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Gets the plain-text email body.
    /// </summary>
    public required string PlainTextBody { get; init; }

    /// <summary>
    /// Gets the basic HTML email body.
    /// </summary>
    public required string HtmlBody { get; init; }
}
