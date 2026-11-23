using Microsoft.Extensions.Logging;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Email;
using Tequio.Domain.Repositories;
using Tequio.Domain.VerificationCodes;

namespace Tequio.Domain.Services;

/// <summary>
/// Contract for authentication and user-management operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Creates a user and sends an account-verification code.
    /// </summary>
    Task<int> RegisterUserAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns a session token.
    /// </summary>
    Task<string> AuthenticateUserAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an account-verification code.
    /// </summary>
    Task VerifyAccountAsync(
        VerifyAccountRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a new account-verification code when the account exists and is pending verification.
    /// </summary>
    Task ResendVerificationCodeAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a password-recovery code when the account exists.
    /// </summary>
    Task SendPasswordRecoveryCodeAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the user password after validating the recovery code.
    /// </summary>
    /// <param name="request">Data containing email, OTP code, and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default);

    Task RegisterAdminAsync(RegisterRequestDto request,
        string masterAdminEmail,
        CancellationToken cancellationToken = default);

    /// <summary>Resends authorization for an existing pending Admin, exclusively through MasterAdmin.</summary>
    Task ResendAdminAuthorizationAsync(string adminEmail, string masterAdminEmail,
        CancellationToken cancellationToken = default);

    Task AuthorizeAdminAsync(AuthorizeAdminRequestDto request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Coordinates authentication, verification-code persistence, and email delivery.
/// </summary>
public class AuthService : IAuthService
{
    private const int VerificationCodeExpirationMinutes = 15;
    private const string DefaultLanguageCode = "es";

    private readonly IAuthRepository _authRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IVerificationCodeGenerator _verificationCodeGenerator;
    private readonly IVerificationCodeRepository _verificationCodeRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    public AuthService(
        IAuthRepository authRepository,
        IJwtProvider jwtProvider,
        IVerificationCodeGenerator verificationCodeGenerator,
        IVerificationCodeRepository verificationCodeRepository,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _jwtProvider = jwtProvider;
        _verificationCodeGenerator = verificationCodeGenerator;
        _verificationCodeRepository = verificationCodeRepository;
        _emailService = emailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<int> RegisterUserAsync(
        RegisterRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.RoleId is not (1 or 2))
        {
            throw new ArgumentException("El registro público solo admite comprador o productor.");
        }
        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        int userId = await _authRepository.CreateUserAsync(request, passwordHash, cancellationToken);

        await GeneratePersistAndSendCodeAsync(
            userId,
            request.Email,
            request.FirstName,
            SecurityCodeEmailType.AccountVerification,
            cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Usuario {Email} registrado exitosamente en el sistema.", request.Email);
        }
        return userId;
    }

    /// <inheritdoc />
    public async Task<string> AuthenticateUserAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var authData = await _authRepository.GetUserAuthDataAsync(request.Email, cancellationToken);

        if (string.IsNullOrEmpty(authData.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(request.Password, authData.PasswordHash))
        {
            throw new UnauthorizedAccessException(ErrorMessages.InvalidCredentials);
        }

        if (!authData.IsVerified)
        {
            string message = authData.RoleId == "3"
                ? "La cuenta está pendiente de autorización por el administrador maestro."
                : "Verifica tu correo electrónico antes de iniciar sesión.";
            throw new UnauthorizedAccessException(message);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Inicio de sesión exitoso y token generado para el usuario {UserId}.", authData.UserId);
        }
        return _jwtProvider.GenerateToken(authData.UserId!, authData.RoleId!);
    }

    /// <inheritdoc />
    public async Task VerifyAccountAsync(
        VerifyAccountRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserVerificationDataAsync(request.Email, cancellationToken);

        if (user is null || user.RoleId is 3 or 4)
        {
            throw new ArgumentException(ErrorMessages.OtpInvalid);
        }

        if (user.IsVerified)
        {
            return;
        }

        await _verificationCodeRepository.ValidateAsync(
            user.UserId,
            request.VerificationCode,
            SecurityCodeEmailType.AccountVerification,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResendVerificationCodeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserVerificationDataAsync(email, cancellationToken);

        if (user is null || user.IsVerified || user.RoleId is 3 or 4)
        {
            return;
        }

        await GeneratePersistAndSendCodeAsync(
            user.UserId,
            email,
            user.FirstName,
            SecurityCodeEmailType.AccountVerification,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPasswordRecoveryCodeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserVerificationDataAsync(email, cancellationToken);

        if (user is null)
        {
            return;
        }

        await GeneratePersistAndSendCodeAsync(
            user.UserId,
            email,
            user.FirstName,
            SecurityCodeEmailType.PasswordRecovery,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task ResetPasswordAsync(
        ResetPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserVerificationDataAsync(request.Email, cancellationToken);

        if (user is null)
        {
            throw new ArgumentException(ErrorMessages.OtpInvalid);
        }

        await _verificationCodeRepository.ValidateAsync(
            user.UserId,
            request.Code,
            SecurityCodeEmailType.PasswordRecovery,
            cancellationToken);

        string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _authRepository.UpdatePasswordAsync(user.UserId, newPasswordHash, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Contraseña restablecida exitosamente para el usuario {Email}.", request.Email);
        }
    }

    public async Task RegisterAdminAsync(RegisterRequestDto request, string masterAdminEmail, CancellationToken cancellationToken = default)
    {
        var existing = await _authRepository.GetUserVerificationDataAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            if (existing.RoleId == 3 && !existing.IsVerified)
            {
                await ResendAdminAuthorizationAsync(request.Email, masterAdminEmail, cancellationToken);
                return;
            }
            throw new ArgumentException(ErrorMessages.EmailAlreadyRegistered);
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        request.RoleId = 3;

        int newAdminId = await _authRepository.CreateUserAsync(request, passwordHash, cancellationToken);

        await GeneratePersistAndSendCodeAsync(
            newAdminId,
            masterAdminEmail,
            "Master Admin",
            SecurityCodeEmailType.AdminAuthorization,
            cancellationToken);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("El Administrador Maestro solicitó la creación del administrador {NewAdminEmail}.", request.Email);
        }
    }

    /// <inheritdoc />
    public async Task ResendAdminAuthorizationAsync(string adminEmail, string masterAdminEmail,
        CancellationToken cancellationToken = default)
    {
        var admin = await _authRepository.GetUserVerificationDataAsync(adminEmail, cancellationToken);
        if (admin is null || admin.RoleId != 3)
        {
            throw new ArgumentException("No se encontró una solicitud pendiente de administrador.");
        }
        if (admin.IsVerified)
        {
            throw new ArgumentException("La cuenta de administrador ya está autorizada.");
        }
        await GeneratePersistAndSendCodeAsync(admin.UserId, masterAdminEmail, "Administrador maestro",
            SecurityCodeEmailType.AdminAuthorization, cancellationToken);
    }

    public async Task AuthorizeAdminAsync(AuthorizeAdminRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserVerificationDataAsync(request.NewAdminEmail, cancellationToken);
        if (user is null || user.RoleId != 3)
        {
            throw new ArgumentException("No se encontró una solicitud de registro para este correo.");
        }

        if (user.IsVerified)
        {
            throw new ArgumentException("Esta cuenta de administrador ya ha sido autorizada.");
        }

        await _verificationCodeRepository.ValidateAsync(
            user.UserId,
            request.VerificationCode,
            SecurityCodeEmailType.AdminAuthorization,
            cancellationToken);

        // Authorization itself is committed atomically by SQL. Do not send a fabricated OTP as a receipt.
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("El Administrador Maestro autorizó la cuenta del nuevo administrador {NewAdminEmail}.", request.NewAdminEmail);
        }
    }

    private async Task GeneratePersistAndSendCodeAsync(
        int userId,
        string recipientEmail,
        string recipientName,
        SecurityCodeEmailType type,
        CancellationToken cancellationToken)
    {
        string verificationCode = _verificationCodeGenerator.GenerateCode();

        await _verificationCodeRepository.SaveAsync(
            userId,
            verificationCode,
            type,
            cancellationToken);

        await _emailService.SendSecurityCodeAsync(
            new SecurityCodeEmailRequest
            {
                RecipientEmail = recipientEmail,
                RecipientName = recipientName,
                VerificationCode = verificationCode,
                Type = type,
                LanguageCode = DefaultLanguageCode,
                ExpirationMinutes = VerificationCodeExpirationMinutes
            },
            cancellationToken);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Código de seguridad enviado para {Purpose}.", type);
        }
    }
}
