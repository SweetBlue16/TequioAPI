using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tequio.Api.Extensions;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Controller responsible for managing user profile operations securely via JWT context.
    /// </summary>
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the profile data of the currently authenticated user.
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            int userId = User.GetUserId();
            var profile = await _userService.GetUserProfileAsync(userId);

            return Ok(profile);
        }

        /// <summary>
        /// Updates the contact and biographic information of the currently authenticated user.
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto request)
        {
            int userId = User.GetUserId();
            _logger.LogInformation("El usuario {UserId} solicitó la actualización de su información de perfil.", userId);

            await _userService.UpdateUserProfileAsync(userId, request);
            return Ok(new
            {
                mensaje = "Perfil actualizado exitosamente."
            });
        }

        /// <summary>
        /// Updates the profile picture URL of the currently authenticated user.
        /// </summary>
        [HttpPut("me/profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfilePictureDto request)
        {
            int userId = User.GetUserId();
            _logger.LogInformation("El usuario {UserId} solicitó la actualización de su foto de perfil.", userId);

            await _userService.UpdateProfilePictureAsync(userId, request.PictureUrl);
            return Ok(new
            {
                mensaje = "Foto de perfil actualizada exitosamente."
            });
        }
    }
}
