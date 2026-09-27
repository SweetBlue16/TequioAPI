using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for authentication and user management operations.
    /// </summary>
    public interface IAuthService
    {
        Task<int> RegisterUserAsync(RegisterRequestDto request);
        Task<string> AuthenticateUserAsync(LoginRequestDto request);
    }

    /// <summary>
    /// Implementation of authentication logic, hashing, and database communication.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtProvider _jwtProvider;

        public AuthService(IAuthRepository authRepository, IJwtProvider jwtProvider)
        {
            _authRepository = authRepository;
            _jwtProvider = jwtProvider;
        }

        public async Task<int> RegisterUserAsync(RegisterRequestDto request)
        {
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            return await _authRepository.CreateUserAsync(request, passwordHash);
        }

        public async Task<string> AuthenticateUserAsync(LoginRequestDto request)
        {
            var authData = await _authRepository.GetUserAuthDataAsync(request.Email);

            if (string.IsNullOrEmpty(authData.PasswordHash) || !BCrypt.Net.BCrypt.Verify(request.Password, authData.PasswordHash))
            {
                throw new UnauthorizedAccessException(ErrorMessages.InvalidCredentials);
            }

            return _jwtProvider.GenerateToken(authData.UserId!, authData.RoleId!);
        }
    }
}
