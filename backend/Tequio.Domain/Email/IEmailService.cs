namespace Tequio.Domain.Email;

/// <summary>
/// Defines asynchronous delivery of security code emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an existing six-digit security code to the requested recipient.
    /// </summary>
    /// <param name="request">The recipient, code, purpose, language, and validity information.</param>
    /// <param name="cancellationToken">A token that can cancel the delivery operation.</param>
    Task SendSecurityCodeAsync(
        SecurityCodeEmailRequest request,
        CancellationToken cancellationToken = default);
}
