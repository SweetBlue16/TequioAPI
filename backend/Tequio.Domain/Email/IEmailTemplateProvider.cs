namespace Tequio.Domain.Email;

/// <summary>
/// Creates localized security code email content without sending it.
/// </summary>
public interface IEmailTemplateProvider
{
    /// <summary>
    /// Creates the subject, plain-text body, and HTML body for a security code email.
    /// </summary>
    /// <param name="request">The dynamic values and language requested for the email.</param>
    /// <returns>Localized email content.</returns>
    EmailContent CreateSecurityCodeEmail(SecurityCodeEmailRequest request);
}
