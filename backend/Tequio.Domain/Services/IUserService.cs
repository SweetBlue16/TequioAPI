using Tequio.Domain.Constants;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for user profile business logic and operations.
    /// </summary>
    public interface IUserService
    {
        Task UpdateProfilePictureAsync(int userId, string pictureUrl);
    }

    /// <summary>
    /// Implementation of user profile logic, orchestrating storage and database updates.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task UpdateProfilePictureAsync(int userId, string pictureUrl)
        {
            await _userRepository.UpdateProfilePictureAsync(userId, pictureUrl);
        }
    }
}
