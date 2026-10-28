using Microsoft.Extensions.Logging;
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
        /// <summary>
        /// Promotes the specified user to the producer role.
        /// </summary>
        /// <param name="userId">Unique identifier of the target user.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PromoteToProducerAsync(int userId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of user profile logic, orchestrating storage and database updates.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
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
            _logger.LogInformation("Delegando actualización de perfil del usuario {UserId} al repositorio.", userId);
            await _userRepository.UpdateUserProfileAsync(userId, request);
        }

        /// <inheritdoc />
        public async Task PromoteToProducerAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            if (userId <= 0)
            {
                throw new ArgumentException(ErrorMessages.RecordNotFound);
            }

            _logger.LogInformation("Iniciando solicitud para promover al usuario {UserId} a rol de productor.", userId);

            await _userRepository.PromoteToProducerAsync(userId, cancellationToken);

            _logger.LogInformation("Usuario {UserId} promovido exitosamente a rol de productor.", userId);
        }

    }
}
