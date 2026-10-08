
using Tequio.Domain.Dtos;

namespace Tequio.Domain.Repositories
{
    /// <summary>
    /// Contract for database operations related to user profile management.
    /// </summary>
    public interface IUserRepository
    {
        Task UpdateProfilePictureAsync(int userId, string profilePictureUrl);
        Task<UserProfileDto> GetUserProfileAsync(int userId);
        Task UpdateUserProfileAsync(int userId, UpdateUserProfileDto request);
    }
}
