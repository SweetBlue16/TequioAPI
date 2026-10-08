using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Tequio.Domain.Email;

namespace Tequio.Infrastructure.Email;

/// <summary>
/// Delivers localized security code emails through an authenticated SMTP connection.
/// </summary>
public sealed class EmailService : IEmailService
{
    private const string DeliveryErrorMessage = "No fue posible enviar el correo en este momento.";

    private readonly EmailOptions _options;
    private readonly IEmailTemplateProvider _templateProvider;
    private readonly ISmtpClientFactory _smtpClientFactory;
    private readonly ILogger<EmailService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailService"/> class.
    /// </summary>
    /// <param name="options">The typed SMTP configuration.</param>
    /// <param name="templateProvider">The localized email content provider.</param>
    /// <param name="smtpClientFactory">The SMTP client factory.</param>
    /// <param name="logger">The technical logger.</param>
    public EmailService(
        IOptions<EmailOptions> options,
        IEmailTemplateProvider templateProvider,
        ISmtpClientFactory smtpClientFactory,
        ILogger<EmailService> logger)
    {
        _options = options.Value;
        _templateProvider = templateProvider;
        _smtpClientFactory = smtpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendSecurityCodeAsync(
        SecurityCodeEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var content = _templateProvider.CreateSecurityCodeEmail(request);
        var message = CreateMessage(request, content);
        var securityOption = _options.UseStartTls
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        _logger.LogInformation("Solicitud de envío de correo de código de seguridad para {EmailType}.", request.Type);
        using var smtpClient = _smtpClientFactory.Create();

        try
        {
            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                securityOption,
                cancellationToken);
            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);
            await smtpClient.SendAsync(message, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Código de seguridad enviado para {EmailType}.", request.Type);
        }
        catch (AuthenticationException exception)
        {
            throw CreateDeliveryException("La autenticación SMTP falló.", exception);
        }
        catch (System.Security.Authentication.AuthenticationException exception)
        {
            throw CreateDeliveryException("La autenticación SMTP TLS falló.", exception);
        }
        catch (SmtpCommandException exception)
        {
            throw CreateDeliveryException("El servidor SMTP rechazó un comando.", exception);
        }
        catch (SmtpProtocolException exception)
        {
            throw CreateDeliveryException("El intercambio de protocolo SMTP falló.", exception);
        }
        catch (SocketException exception)
        {
            throw CreateDeliveryException("El servidor SMTP no pudo ser alcanzado.", exception);
        }
        catch (IOException exception)
        {
            throw CreateDeliveryException("La conexión SMTP falló.", exception);
        }
    }

    private static void ValidateRequest(SecurityCodeEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RecipientEmail) ||
            !request.RecipientEmail.Contains('@', StringComparison.Ordinal) ||
            !MailboxAddress.TryParse(request.RecipientEmail, out _))
        {
            throw new ArgumentException("El correo electrónico del destinatario no es válido.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.VerificationCode) ||
            request.VerificationCode.Length != 6 ||
            request.VerificationCode.Any(character => !char.IsAsciiDigit(character)))
        {
            throw new ArgumentException("El código de seguridad debe tener 6 dígitos.", nameof(request));
        }

        if (!Enum.IsDefined(request.Type))
        {
            throw new ArgumentException("El tipo de correo no es válido.", nameof(request));
        }

        if (request.ExpirationMinutes <= 0)
        {
            throw new ArgumentException("La vigencia del código debe ser mayor que cero.", nameof(request));
        }
    }

    private MimeMessage CreateMessage(SecurityCodeEmailRequest request, EmailContent content)
    {
        var message = new MimeMessage
        {
            Subject = content.Subject,
            Body = new BodyBuilder
            {
                TextBody = content.PlainTextBody,
                HtmlBody = content.HtmlBody
            }.ToMessageBody()
        };

        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(new MailboxAddress(request.RecipientName, request.RecipientEmail));

        return message;
    }

    private EmailDeliveryException CreateDeliveryException(string technicalMessage, Exception exception)
    {
        _logger.LogError(exception, "{TechnicalMessage}", technicalMessage);
        return new EmailDeliveryException(DeliveryErrorMessage, exception);
    }
}
