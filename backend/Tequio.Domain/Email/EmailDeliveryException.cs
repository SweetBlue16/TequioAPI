namespace Tequio.Domain.Email;

/// <summary>
/// Represents an email delivery failure that is safe to expose without provider details.
/// </summary>
public sealed class EmailDeliveryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailDeliveryException"/> class.
    /// </summary>
    /// <param name="message">A non-sensitive description of the failure.</param>
    /// <param name="innerException">The provider exception retained for technical diagnostics.</param>
    public EmailDeliveryException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
