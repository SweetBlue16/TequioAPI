using Tequio.Domain.Email;
using Xunit;

namespace Tequio.Tests.Email;

/// <summary>
/// Verifies localized security code email content.
/// </summary>
public sealed class EmailTemplateProviderTest
{
    private readonly EmailTemplateProvider _templateProvider = new();

    /// <summary>
    /// Verifies that Spanish content uses clear language and the requested dynamic values.
    /// </summary>
    [Fact]
    public void TestCreateSecurityCodeEmailWithSpanishLanguageShouldCreateNaturalSpanishContent()
    {
        var request = CreateRequest(SecurityCodeEmailType.AccountVerification, "es");

        var content = _templateProvider.CreateSecurityCodeEmail(request);

        Assert.Equal("Tu código de verificación de Tequio", content.Subject);
        Assert.Contains("Hola, Rodrigo:", content.PlainTextBody);
        Assert.Contains("123456", content.PlainTextBody);
        Assert.Contains("15 minutos", content.PlainTextBody);
        Assert.Contains("puedes ignorar este correo", content.PlainTextBody);
    }

    /// <summary>
    /// Verifies that English content is selected for a regional English language code.
    /// </summary>
    [Fact]
    public void TestCreateSecurityCodeEmailWithEnglishLanguageShouldCreateNaturalEnglishContent()
    {
        var request = CreateRequest(SecurityCodeEmailType.PasswordRecovery, "en-US");

        var content = _templateProvider.CreateSecurityCodeEmail(request);

        Assert.Equal("Your code to recover your Tequio account", content.Subject);
        Assert.Contains("Hello, Rodrigo:", content.PlainTextBody);
        Assert.Contains("account recovery code", content.PlainTextBody);
        Assert.Contains("15 minutes", content.PlainTextBody);
    }

    /// <summary>
    /// Verifies that unsupported languages fall back to Spanish.
    /// </summary>
    [Fact]
    public void TestCreateSecurityCodeEmailWithUnsupportedLanguageShouldFallBackToSpanish()
    {
        var request = CreateRequest(SecurityCodeEmailType.TwoFactorAuthentication, "fr");

        var content = _templateProvider.CreateSecurityCodeEmail(request);

        Assert.Equal("Tu código de seguridad de Tequio", content.Subject);
        Assert.Contains("Este código estará disponible", content.PlainTextBody);
    }

    /// <summary>
    /// Verifies that dynamic names cannot inject HTML into an email.
    /// </summary>
    [Fact]
    public void TestCreateSecurityCodeEmailWithUnsafeNameShouldEncodeHtmlContent()
    {
        var request = new SecurityCodeEmailRequest
        {
            RecipientEmail = "recipient@example.com",
            RecipientName = "<Rodrigo>",
            VerificationCode = "123456",
            Type = SecurityCodeEmailType.AdminAuthorization
        };

        var content = _templateProvider.CreateSecurityCodeEmail(request);

        Assert.Contains("&lt;Rodrigo&gt;", content.HtmlBody);
        Assert.DoesNotContain("<Rodrigo>", content.HtmlBody);
    }

    /// <summary>
    /// Verifies that each supported purpose receives purpose-specific wording.
    /// </summary>
    [Theory]
    [InlineData(SecurityCodeEmailType.AccountVerification, "código de verificación")]
    [InlineData(SecurityCodeEmailType.PasswordRecovery, "código para recuperar tu cuenta")]
    [InlineData(SecurityCodeEmailType.AdminAuthorization, "código de autorización")]
    [InlineData(SecurityCodeEmailType.TwoFactorAuthentication, "código de seguridad")]
    public void TestCreateSecurityCodeEmailWithSupportedTypeShouldCreatePurposeSpecificContent(
        SecurityCodeEmailType type,
        string expectedDescription)
    {
        var request = CreateRequest(type, "es");

        var content = _templateProvider.CreateSecurityCodeEmail(request);

        Assert.Contains(expectedDescription, content.PlainTextBody);
    }

    private static SecurityCodeEmailRequest CreateRequest(SecurityCodeEmailType type, string languageCode)
    {
        return new SecurityCodeEmailRequest
        {
            RecipientEmail = "recipient@example.com",
            RecipientName = "Rodrigo",
            VerificationCode = "123456",
            Type = type,
            LanguageCode = languageCode
        };
    }
}
