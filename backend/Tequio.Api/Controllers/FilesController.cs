using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Controller responsible for handling multimedia uploads.
    /// </summary>
    [ApiController]
    [Route("api/v1/files")]
    [Authorize]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        /// <summary>
        /// Uploads an image file to the cloud storage provider and returns its public URL.
        /// </summary>
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No se proporcionó ninguna imagen válida." });
            }

            using var stream = file.OpenReadStream();
            string url = await _fileService.UploadImageAsync(stream, file.FileName);

            return Ok(new
            {
                mensaje = "Archivo subido exitosamente.",
                url = url
            });
        }
    }
}
