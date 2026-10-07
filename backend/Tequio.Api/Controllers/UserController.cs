using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        /// Uploads and updates the user's profile picture.
        /// </summary>
        [HttpPost("{id}/profile-picture")]
        public async Task<IActionResult> UploadProfilePicture(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No se proporcionó ninguna imagen válida." });
            }

            using var stream = file.OpenReadStream();

            string url = await _userService.UploadProfilePictureAsync(id, stream, file.FileName);

            return Ok(new
            {
                mensaje = "Foto de perfil actualizada exitosamente.",
                url = url
            });
        }
    }
}
