using Tequio.Domain.Constants;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for user profile business logic and operations.
    /// </summary>
    public interface IUserService
    {
        Task<string> UploadProfilePictureAsync(int userId, Stream imageStream, string fileName);
    }

    /// <summary>
    /// Implementation of user profile logic, orchestrating storage and database updates.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStorageService _storageService;

        public UserService(IUserRepository userRepository, IStorageService storageService)
        {
            _userRepository = userRepository;
            _storageService = storageService;
        }

        public async Task<string> UploadProfilePictureAsync(int userId, Stream imageStream, string fileName)
        {
            const long maxFileSize = 5 * 1024 * 1024;

            if (imageStream.Length > maxFileSize)
            {
                throw new ArgumentException(ErrorMessages.FileTooLarge);
            }

            string imageUrl = await _storageService.UploadFileAsync(imageStream, fileName);

            await _userRepository.UpdateProfilePictureAsync(userId, imageUrl);

            return imageUrl;
        }
    }
}
