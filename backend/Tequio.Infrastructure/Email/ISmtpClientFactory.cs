using MailKit.Net.Smtp;

namespace Tequio.Infrastructure.Email;

/// <summary>
/// Creates isolated MailKit SMTP clients for email delivery operations.
/// </summary>
public interface ISmtpClientFactory
{
    /// <summary>
    /// Creates an SMTP client owned by the caller.
    /// </summary>
    /// <returns>A new SMTP client.</returns>
    ISmtpClient Create();
}
