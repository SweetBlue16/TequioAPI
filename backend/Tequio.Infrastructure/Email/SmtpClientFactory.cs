using MailKit.Net.Smtp;

namespace Tequio.Infrastructure.Email;

/// <summary>
/// Creates MailKit SMTP clients for production email delivery.
/// </summary>
public sealed class SmtpClientFactory : ISmtpClientFactory
{
    /// <inheritdoc />
    public ISmtpClient Create()
    {
        return new SmtpClient();
    }
}
