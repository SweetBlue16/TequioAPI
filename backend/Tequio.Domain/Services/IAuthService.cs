using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Email;
using Tequio.Domain.Repositories;
using Tequio.Domain.VerificationCodes;

namespace Tequio.Domain.Services
{
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthService"/> class.
        /// </summary>
        public AuthService(
            IAuthRepository authRepository,
            IJwtProvider jwtProvider,
            IVerificationCodeGenerator verificationCodeGenerator,
            IVerificationCodeRepository verificationCodeRepository,
            IEmailService emailService)
        {
            _authRepository = authRepository;
            _jwtProvider = jwtProvider;
            _verificationCodeGenerator = verificationCodeGenerator;
            _verificationCodeRepository = verificationCodeRepository;
            _emailService = emailService;
        }

        /// <inheritdoc />
        public async Task<int> RegisterUserAsync(
            RegisterRequestDto request,
            CancellationToken cancellationToken = default)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            int userId = await _authRepository.CreateUserAsync(request, passwordHash, cancellationToken);

            await GeneratePersistAndSendCodeAsync(
                userId,
                request.Email,
                request.FirstName,
                SecurityCodeEmailType.AccountVerification,
                cancellationToken);

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

            return _jwtProvider.GenerateToken(authData.UserId!, authData.RoleId!);
        }

        /// <inheritdoc />
        public async Task VerifyAccountAsync(
            VerifyAccountRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var user = await _authRepository.GetUserVerificationDataAsync(request.Email, cancellationToken);

            if (user is null)
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

            if (user is null || user.IsVerified)
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
        }
    }
}
