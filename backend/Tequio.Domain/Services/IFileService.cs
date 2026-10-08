using Microsoft.Extensions.Logging;
using Tequio.Domain.Constants;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for file management business logic.
    /// </summary>
    public interface IFileService
    {
        Task<string> UploadImageAsync(Stream imageStream, string fileName);
    }

    /// <summary>
    /// Implementation of file management logic, applying business rules.
    /// </summary>
    public class FileService : IFileService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;

        private readonly IStorageService _storageService;
        private readonly ILogger<FileService> _logger;

        public FileService(IStorageService storageService, ILogger<FileService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            if (imageStream.Length > MaxFileSize)
            {
                throw new ArgumentException(ErrorMessages.FileTooLarge);
            }

            _logger.LogInformation("El archivo {FileName} aprobó las validaciones de negocio. Iniciando transferencia a la nube.", fileName);
            return await _storageService.UploadFileAsync(imageStream, fileName);
        }
    }
}
