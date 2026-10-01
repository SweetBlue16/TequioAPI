using Moq;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Email;
using Tequio.Domain.Models;
using Tequio.Domain.Repositories;
using Tequio.Domain.Services;
using Tequio.Domain.VerificationCodes;
using Xunit;

namespace Tequio.Tests.Auth;

/// <summary>
/// Tests for verification-code orchestration in <see cref="AuthService"/>.
/// </summary>
public sealed class AuthServiceTest
{
    private const string Email = "buyer@example.test";
    private const string Code = "004281";

    private readonly Mock<IAuthRepository> _authRepository = new();
    private readonly Mock<IJwtProvider> _jwtProvider = new();
    private readonly Mock<IVerificationCodeGenerator> _codeGenerator = new();
    private readonly Mock<IVerificationCodeRepository> _codeRepository = new();
    private readonly Mock<IEmailService> _emailService = new();

    /// <summary>
    /// Verifies registration creates the user, generates and persists a code, and then sends it.
    /// </summary>
    [Fact]
    public async Task TestRegisterUserWithValidDataShouldPersistCodeBeforeSendingEmail()
    {
        var operations = new List<string>();
        RegisterRequestDto request = CreateRegisterRequest();

        _authRepository
            .Setup(repository => repository.CreateUserAsync(
                request,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("create-user"))
            .ReturnsAsync(42);
        _codeGenerator
            .Setup(generator => generator.GenerateCode())
            .Callback(() => operations.Add("generate-code"))
            .Returns(Code);
        _codeRepository
            .Setup(repository => repository.SaveAsync(
                42,
                Code,
                SecurityCodeEmailType.AccountVerification,
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("persist-code"))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(service => service.SendSecurityCodeAsync(
                It.Is<SecurityCodeEmailRequest>(emailRequest =>
                    emailRequest.RecipientEmail == Email &&
                    emailRequest.RecipientName == "Ana" &&
                    emailRequest.VerificationCode == Code &&
                    emailRequest.Type == SecurityCodeEmailType.AccountVerification &&
                    emailRequest.LanguageCode == "es" &&
                    emailRequest.ExpirationMinutes == 15),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("send-email"))
            .Returns(Task.CompletedTask);

        int userId = await CreateService().RegisterUserAsync(request);

        Assert.Equal(42, userId);
        Assert.Equal(
            ["create-user", "generate-code", "persist-code", "send-email"],
            operations);
        _authRepository.Verify(repository => repository.CreateUserAsync(
            request,
            It.Is<string>(hash => BCrypt.Net.BCrypt.Verify(request.Password, hash)),
            It.IsAny<CancellationToken>()), Times.Once);
        _emailService.VerifyAll();
    }

    /// <summary>
    /// Verifies an SMTP failure is propagated after the persisted code remains available for resend.
    /// </summary>
    [Fact]
    public async Task TestRegisterUserWithEmailFailureShouldPropagateDeliveryExceptionAfterPersistence()
    {
        RegisterRequestDto request = CreateRegisterRequest();
        var expectedException = new EmailDeliveryException(
            "No fue posible enviar el correo.",
            new TimeoutException());

        SetupRegistrationBeforeEmail(request);
        _emailService
            .Setup(service => service.SendSecurityCodeAsync(
                It.IsAny<SecurityCodeEmailRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        EmailDeliveryException exception = await Assert.ThrowsAsync<EmailDeliveryException>(
            () => CreateService().RegisterUserAsync(request));

        Assert.Same(expectedException, exception);
        _codeRepository.Verify(repository => repository.SaveAsync(
            42,
            Code,
            SecurityCodeEmailType.AccountVerification,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies no email is sent when code persistence fails.
    /// </summary>
    [Fact]
    public async Task TestRegisterUserWithPersistenceFailureShouldNotSendEmail()
    {
        RegisterRequestDto request = CreateRegisterRequest();
        _authRepository
            .Setup(repository => repository.CreateUserAsync(
                request,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _codeGenerator.Setup(generator => generator.GenerateCode()).Returns(Code);
        _codeRepository
            .Setup(repository => repository.SaveAsync(
                42,
                Code,
                SecurityCodeEmailType.AccountVerification,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(ErrorMessages.DatabaseError));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().RegisterUserAsync(request));

        _emailService.Verify(service => service.SendSecurityCodeAsync(
            It.IsAny<SecurityCodeEmailRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies account verification delegates to the database procedure with the correct purpose.
    /// </summary>
    [Fact]
    public async Task TestVerifyAccountWithValidCodeShouldValidateAccountVerificationCode()
    {
        var request = new VerifyAccountRequestDto
        {
            Email = Email,
            VerificationCode = Code
        };
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVerificationData(42, "Ana", false));
        _codeRepository
            .Setup(repository => repository.ValidateAsync(
                42,
                Code,
                SecurityCodeEmailType.AccountVerification,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await CreateService().VerifyAccountAsync(request);

        _codeRepository.VerifyAll();
    }

    /// <summary>
    /// Verifies an invalid code response from the database remains sanitized.
    /// </summary>
    [Fact]
    public async Task TestVerifyAccountWithInvalidCodeShouldPropagateSanitizedError()
    {
        var request = new VerifyAccountRequestDto { Email = Email, VerificationCode = Code };
        SetupUnverifiedUser();
        _codeRepository
            .Setup(repository => repository.ValidateAsync(
                42,
                Code,
                SecurityCodeEmailType.AccountVerification,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(ErrorMessages.OtpInvalid));

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().VerifyAccountAsync(request));

        Assert.Equal(ErrorMessages.OtpInvalid, exception.Message);
    }

    /// <summary>
    /// Verifies an expired code response from the database remains distinct and sanitized.
    /// </summary>
    [Fact]
    public async Task TestVerifyAccountWithExpiredCodeShouldPropagateExpiredError()
    {
        var request = new VerifyAccountRequestDto { Email = Email, VerificationCode = Code };
        SetupUnverifiedUser();
        _codeRepository
            .Setup(repository => repository.ValidateAsync(
                42,
                Code,
                SecurityCodeEmailType.AccountVerification,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException(ErrorMessages.OtpExpired));

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().VerifyAccountAsync(request));

        Assert.Equal(ErrorMessages.OtpExpired, exception.Message);
    }

    /// <summary>
    /// Verifies a missing account receives the same sanitized response as an invalid code.
    /// </summary>
    [Fact]
    public async Task TestVerifyAccountWithMissingUserShouldReturnInvalidCodeError()
    {
        var request = new VerifyAccountRequestDto { Email = Email, VerificationCode = Code };
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVerificationData?)null);

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => CreateService().VerifyAccountAsync(request));

        Assert.Equal(ErrorMessages.OtpInvalid, exception.Message);
        _codeRepository.Verify(repository => repository.ValidateAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<SecurityCodeEmailType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies an already verified account is handled idempotently.
    /// </summary>
    [Fact]
    public async Task TestVerifyAccountWithVerifiedUserShouldNotValidateAnotherCode()
    {
        var request = new VerifyAccountRequestDto { Email = Email, VerificationCode = Code };
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVerificationData(42, "Ana", true));

        await CreateService().VerifyAccountAsync(request);

        _codeRepository.Verify(repository => repository.ValidateAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<SecurityCodeEmailType>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies resend creates and delivers a replacement account-verification code.
    /// </summary>
    [Fact]
    public async Task TestResendVerificationCodeWithPendingUserShouldPersistAndSendNewCode()
    {
        SetupUnverifiedUser();
        SetupGeneratedCode(SecurityCodeEmailType.AccountVerification);

        await CreateService().ResendVerificationCodeAsync(Email);

        _codeRepository.Verify(repository => repository.SaveAsync(
            42,
            Code,
            SecurityCodeEmailType.AccountVerification,
            It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(service => service.SendSecurityCodeAsync(
            It.Is<SecurityCodeEmailRequest>(request =>
                request.VerificationCode == Code &&
                request.Type == SecurityCodeEmailType.AccountVerification),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies resend does not reveal or process a missing account.
    /// </summary>
    [Fact]
    public async Task TestResendVerificationCodeWithMissingUserShouldPerformNoCodeOperations()
    {
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVerificationData?)null);

        await CreateService().ResendVerificationCodeAsync(Email);

        VerifyNoCodeOperations();
    }

    /// <summary>
    /// Verifies resend does not issue another code for an account that is already verified.
    /// </summary>
    [Fact]
    public async Task TestResendVerificationCodeWithVerifiedUserShouldPerformNoCodeOperations()
    {
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVerificationData(42, "Ana", true));

        await CreateService().ResendVerificationCodeAsync(Email);

        VerifyNoCodeOperations();
    }

    /// <summary>
    /// Verifies recovery uses an isolated password-recovery code purpose.
    /// </summary>
    [Fact]
    public async Task TestSendPasswordRecoveryCodeWithExistingUserShouldUsePasswordRecoveryPurpose()
    {
        SetupUnverifiedUser();
        SetupGeneratedCode(SecurityCodeEmailType.PasswordRecovery);

        await CreateService().SendPasswordRecoveryCodeAsync(Email);

        _codeRepository.Verify(repository => repository.SaveAsync(
            42,
            Code,
            SecurityCodeEmailType.PasswordRecovery,
            It.IsAny<CancellationToken>()), Times.Once);
        _emailService.Verify(service => service.SendSecurityCodeAsync(
            It.Is<SecurityCodeEmailRequest>(request =>
                request.VerificationCode == Code &&
                request.Type == SecurityCodeEmailType.PasswordRecovery),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies recovery does not reveal or process a missing account.
    /// </summary>
    [Fact]
    public async Task TestSendPasswordRecoveryCodeWithMissingUserShouldPerformNoCodeOperations()
    {
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserVerificationData?)null);

        await CreateService().SendPasswordRecoveryCodeAsync(Email);

        VerifyNoCodeOperations();
    }

    private AuthService CreateService()
    {
        return new AuthService(
            _authRepository.Object,
            _jwtProvider.Object,
            _codeGenerator.Object,
            _codeRepository.Object,
            _emailService.Object);
    }

    private static RegisterRequestDto CreateRegisterRequest()
    {
        return new RegisterRequestDto
        {
            Email = Email,
            Password = "SecurePassword123!",
            FirstName = "Ana",
            PaternalLastName = "López",
            BirthDate = new DateTime(1990, 1, 1),
            RoleId = 1
        };
    }

    private void SetupRegistrationBeforeEmail(RegisterRequestDto request)
    {
        _authRepository
            .Setup(repository => repository.CreateUserAsync(
                request,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _codeGenerator.Setup(generator => generator.GenerateCode()).Returns(Code);
        _codeRepository
            .Setup(repository => repository.SaveAsync(
                42,
                Code,
                SecurityCodeEmailType.AccountVerification,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupUnverifiedUser()
    {
        _authRepository
            .Setup(repository => repository.GetUserVerificationDataAsync(
                Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserVerificationData(42, "Ana", false));
    }

    private void SetupGeneratedCode(SecurityCodeEmailType type)
    {
        _codeGenerator.Setup(generator => generator.GenerateCode()).Returns(Code);
        _codeRepository
            .Setup(repository => repository.SaveAsync(
                42,
                Code,
                type,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailService
            .Setup(service => service.SendSecurityCodeAsync(
                It.IsAny<SecurityCodeEmailRequest>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void VerifyNoCodeOperations()
    {
        _codeGenerator.Verify(generator => generator.GenerateCode(), Times.Never);
        _codeRepository.Verify(repository => repository.SaveAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<SecurityCodeEmailType>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _emailService.Verify(service => service.SendSecurityCodeAsync(
            It.IsAny<SecurityCodeEmailRequest>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
