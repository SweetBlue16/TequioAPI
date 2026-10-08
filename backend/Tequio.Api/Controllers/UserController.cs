using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Controller responsible for managing user profile operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/users")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Updates the user's profile picture URL.
        /// </summary>
        [HttpPut("{id}/profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture(int id, [FromBody] UpdateProfilePictureDto request)
        {
            await _userService.UpdateProfilePictureAsync(id, request.PictureUrl);

            return Ok(new
            {
                mensaje = "Foto de perfil actualizada exitosamente."
            });
        }
    }
}
