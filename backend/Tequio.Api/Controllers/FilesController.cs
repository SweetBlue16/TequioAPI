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
        private readonly ILogger<FilesController> _logger;

        public FilesController(IFileService fileService, ILogger<FilesController> logger)
        {
            _fileService = fileService;
            _logger = logger;
        }

        /// <summary>
        /// Uploads an image file to the cloud storage provider and returns its public URL.
        /// </summary>
        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            _logger.LogInformation("Petición de subida de archivo recibida: {FileName}, Tamaño: {Size} bytes.", file?.FileName, file?.Length);

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
