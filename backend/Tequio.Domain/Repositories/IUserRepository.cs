
namespace Tequio.Domain.Repositories
{
    /// <summary>
    /// Contract for database operations related to user profile management.
    /// </summary>
    public interface IUserRepository
    {
        Task UpdateProfilePictureAsync(int userId, string profilePictureUrl);
    }
}
