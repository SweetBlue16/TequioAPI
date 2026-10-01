using System.Net;

namespace Tequio.Domain.Email;

/// <summary>
/// Provides simple Spanish and English templates for security code emails.
/// </summary>
public sealed class EmailTemplateProvider : IEmailTemplateProvider
{
    /// <inheritdoc />
    public EmailContent CreateSecurityCodeEmail(SecurityCodeEmailRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return IsEnglish(request.LanguageCode)
            ? CreateEnglishContent(request)
            : CreateSpanishContent(request);
    }

    private static EmailContent CreateSpanishContent(SecurityCodeEmailRequest request)
    {
        var (subject, codeDescription) = request.Type switch
        {
            SecurityCodeEmailType.AccountVerification =>
                ("Tu código de verificación de Tequio", "código de verificación"),
            SecurityCodeEmailType.PasswordRecovery =>
                ("Tu código para recuperar tu cuenta de Tequio", "código para recuperar tu cuenta"),
            SecurityCodeEmailType.AdminAuthorization =>
                ("Tu código de autorización de Tequio", "código de autorización"),
            SecurityCodeEmailType.TwoFactorAuthentication =>
                ("Tu código de seguridad de Tequio", "código de seguridad"),
            _ => throw new ArgumentOutOfRangeException(nameof(request), "El tipo de correo no es válido.")
        };

        var greeting = string.IsNullOrWhiteSpace(request.RecipientName)
            ? "Hola:"
            : $"Hola, {request.RecipientName.Trim()}:";

        var plainTextBody = $"""
            {greeting}

            Tu {codeDescription} de Tequio es:

            {request.VerificationCode}

            Este código estará disponible durante {request.ExpirationMinutes} minutos.

            Si tú no solicitaste este código, puedes ignorar este correo.
            """;

        return new EmailContent
        {
            Subject = subject,
            PlainTextBody = plainTextBody,
            HtmlBody = CreateHtmlBody(
                "es",
                greeting,
                $"Tu {codeDescription} de Tequio es:",
                request.VerificationCode,
                $"Este código estará disponible durante {request.ExpirationMinutes} minutos.",
                "Si tú no solicitaste este código, puedes ignorar este correo.")
        };
    }

    private static EmailContent CreateEnglishContent(SecurityCodeEmailRequest request)
    {
        var (subject, codeDescription) = request.Type switch
        {
            SecurityCodeEmailType.AccountVerification =>
                ("Your Tequio verification code", "verification code"),
            SecurityCodeEmailType.PasswordRecovery =>
                ("Your code to recover your Tequio account", "account recovery code"),
            SecurityCodeEmailType.AdminAuthorization =>
                ("Your Tequio authorization code", "authorization code"),
            SecurityCodeEmailType.TwoFactorAuthentication =>
                ("Your Tequio security code", "security code"),
            _ => throw new ArgumentOutOfRangeException(nameof(request), "The email type is not valid.")
        };

        var greeting = string.IsNullOrWhiteSpace(request.RecipientName)
            ? "Hello:"
            : $"Hello, {request.RecipientName.Trim()}:";

        var plainTextBody = $"""
            {greeting}

            Your Tequio {codeDescription} is:

            {request.VerificationCode}

            This code will be available for {request.ExpirationMinutes} minutes.

            If you did not request this code, you can ignore this email.
            """;

        return new EmailContent
        {
            Subject = subject,
            PlainTextBody = plainTextBody,
            HtmlBody = CreateHtmlBody(
                "en",
                greeting,
                $"Your Tequio {codeDescription} is:",
                request.VerificationCode,
                $"This code will be available for {request.ExpirationMinutes} minutes.",
                "If you did not request this code, you can ignore this email.")
        };
    }

    private static string CreateHtmlBody(
        string languageCode,
        string greeting,
        string introduction,
        string verificationCode,
        string expirationMessage,
        string ignoredRequestMessage)
    {
        return $"""
            <!doctype html>
            <html lang="{languageCode}">
            <body style="font-family: Arial, sans-serif; color: #18110A; line-height: 1.5;">
                <p>{WebUtility.HtmlEncode(greeting)}</p>
                <p>{WebUtility.HtmlEncode(introduction)}</p>
                <p style="font-size: 28px; font-weight: bold; letter-spacing: 6px;">
                    {WebUtility.HtmlEncode(verificationCode)}
                </p>
                <p>{WebUtility.HtmlEncode(expirationMessage)}</p>
                <p>{WebUtility.HtmlEncode(ignoredRequestMessage)}</p>
            </body>
            </html>
            """;
    }

    private static bool IsEnglish(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return false;
        }

        var primaryLanguage = languageCode.Split('-', '_')[0];
        return primaryLanguage.Equals("en", StringComparison.OrdinalIgnoreCase);
    }
}
