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
        private readonly IStorageService _storageService;

        public FileService(IStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            const long maxFileSize = 5 * 1024 * 1024; // 5 MB

            if (imageStream.Length > maxFileSize)
            {
                throw new ArgumentException(ErrorMessages.FileTooLarge);
            }

            return await _storageService.UploadFileAsync(imageStream, fileName);
        }
    }
}
