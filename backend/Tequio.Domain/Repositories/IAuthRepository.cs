using Tequio.Domain.Dtos;

namespace Tequio.Domain.Repositories
{
    /// <summary>
    /// Contract for database operations related to authentication.
    /// </summary>
    public interface IAuthRepository
    {
        Task<int> CreateUserAsync(RegisterRequestDto request, string passwordHash);
        Task<(string? UserId, string? RoleId, string? PasswordHash)> GetUserAuthDataAsync(string email);
    }
}
