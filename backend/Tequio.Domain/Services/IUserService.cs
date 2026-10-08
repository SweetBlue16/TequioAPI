using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for user profile business logic and operations.
    /// </summary>
    public interface IUserService
    {
        Task UpdateProfilePictureAsync(int userId, string pictureUrl);
        Task<UserProfileDto> GetUserProfileAsync(int userId);
        Task UpdateUserProfileAsync(int userId, UpdateUserProfileDto request);
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

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            return await _userRepository.GetUserProfileAsync(userId);
        }

        public async Task UpdateProfilePictureAsync(int userId, string pictureUrl)
        {
            await _userRepository.UpdateProfilePictureAsync(userId, pictureUrl);
        }

        public async Task UpdateUserProfileAsync(int userId, UpdateUserProfileDto request)
        {
            await _userRepository.UpdateUserProfileAsync(userId, request);
        }
    }
}
