using System.Net.Sockets;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MimeKit;
using Moq;
using Tequio.Domain.Email;
using Tequio.Infrastructure.Email;
using Xunit;

namespace Tequio.Tests.Email;

/// <summary>
/// Verifies SMTP email composition, validation, and error handling without a network connection.
/// </summary>
public sealed class EmailServiceTest
{
    /// <summary>
    /// Verifies authenticated STARTTLS delivery and multipart message composition.
    /// </summary>
    [Fact]
    public async Task TestSendSecurityCodeWithValidDataShouldSendMultipartEmailUsingStartTls()
    {
        MimeMessage? sentMessage = null;
        var cancellationToken = new CancellationTokenSource().Token;
        var smtpClient = CreateSuccessfulSmtpClient(message => sentMessage = message);
        var service = CreateService(smtpClient.Object);
        var request = CreateRequest();

        await service.SendSecurityCodeAsync(request, cancellationToken);

        smtpClient.Verify(
            client => client.ConnectAsync(
                "smtp.gmail.com",
                587,
                SecureSocketOptions.StartTls,
                cancellationToken),
            Times.Once);
        smtpClient.Verify(
            client => client.AuthenticateAsync(
                "sender@example.com",
                "non-secret-test-value",
                cancellationToken),
            Times.Once);
        smtpClient.Verify(client => client.DisconnectAsync(true, cancellationToken), Times.Once);

        Assert.NotNull(sentMessage);
        Assert.Equal("sender@example.com", sentMessage.From.Mailboxes.Single().Address);
        Assert.Equal("recipient@example.com", sentMessage.To.Mailboxes.Single().Address);
        Assert.Contains("123456", sentMessage.TextBody);
        Assert.Contains("123456", sentMessage.HtmlBody);
    }

    /// <summary>
    /// Verifies that codes outside the six-digit format are rejected before SMTP is used.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("12345A")]
    [InlineData("1234567")]
    public async Task TestSendSecurityCodeWithInvalidCodeShouldRejectRequest(string verificationCode)
    {
        var smtpClientFactory = new Mock<ISmtpClientFactory>();
        var service = CreateService(smtpClientFactory.Object);
        var request = CreateRequest(verificationCode);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SendSecurityCodeAsync(request, CancellationToken.None));

        Assert.Contains("6 dígitos", exception.Message);
        smtpClientFactory.Verify(factory => factory.Create(), Times.Never);
    }

    /// <summary>
    /// Verifies that invalid recipient addresses are rejected before SMTP is used.
    /// </summary>
    [Fact]
    public async Task TestSendSecurityCodeWithInvalidEmailShouldRejectRequest()
    {
        var smtpClientFactory = new Mock<ISmtpClientFactory>();
        var service = CreateService(smtpClientFactory.Object);
        var request = new SecurityCodeEmailRequest
        {
            RecipientEmail = "invalid-email",
            VerificationCode = "123456",
            Type = SecurityCodeEmailType.AccountVerification
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SendSecurityCodeAsync(request, CancellationToken.None));

        Assert.Contains("correo electrónico", exception.Message);
        smtpClientFactory.Verify(factory => factory.Create(), Times.Never);
    }

    /// <summary>
    /// Verifies that provider connection details are not exposed after a network failure.
    /// </summary>
    [Fact]
    public async Task TestSendSecurityCodeWithNetworkFailureShouldReturnSafeDeliveryError()
    {
        var smtpClient = new Mock<ISmtpClient>();
        smtpClient
            .Setup(client => client.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SocketException());
        var service = CreateService(smtpClient.Object);

        var exception = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => service.SendSecurityCodeAsync(
                CreateRequest(),
                CancellationToken.None));

        Assert.Equal("No fue posible enviar el correo en este momento.", exception.Message);
        Assert.IsType<SocketException>(exception.InnerException);
        Assert.DoesNotContain("smtp.gmail.com", exception.Message);
    }

    private static EmailService CreateService(ISmtpClient smtpClient)
    {
        var smtpClientFactory = new Mock<ISmtpClientFactory>();
        smtpClientFactory.Setup(factory => factory.Create()).Returns(smtpClient);

        return CreateService(smtpClientFactory.Object);
    }

    private static EmailService CreateService(ISmtpClientFactory smtpClientFactory)
    {
        var options = Options.Create(new EmailOptions
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "sender@example.com",
            Password = "non-secret-test-value",
            FromAddress = "sender@example.com",
            FromName = "Tequio",
            UseStartTls = true
        });

        return new EmailService(
            options,
            new EmailTemplateProvider(),
            smtpClientFactory,
            NullLogger<EmailService>.Instance);
    }

    private static Mock<ISmtpClient> CreateSuccessfulSmtpClient(Action<MimeMessage> captureMessage)
    {
        var smtpClient = new Mock<ISmtpClient>();
        smtpClient
            .Setup(client => client.ConnectAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<SecureSocketOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtpClient
            .Setup(client => client.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtpClient
            .Setup(client => client.SendAsync(
                It.IsAny<MimeMessage>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<ITransferProgress>()))
            .Callback<MimeMessage, CancellationToken, ITransferProgress>(
                (message, _, _) => captureMessage(message))
            .ReturnsAsync("accepted");
        smtpClient
            .Setup(client => client.DisconnectAsync(
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return smtpClient;
    }

    private static SecurityCodeEmailRequest CreateRequest(string verificationCode = "123456")
    {
        return new SecurityCodeEmailRequest
        {
            RecipientEmail = "recipient@example.com",
            RecipientName = "Rodrigo",
            VerificationCode = verificationCode,
            Type = SecurityCodeEmailType.AccountVerification
        };
    }
}
