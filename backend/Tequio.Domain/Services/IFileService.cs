using Microsoft.Extensions.Logging;
using Tequio.Domain.Constants;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for file management business logic.
    /// </summary>
    public interface IFileService
    {
        /// <summary>Validates the image type and size, then delegates storage.</summary>
        /// <param name="imageStream">A readable, seekable image stream.</param>
        /// <param name="fileName">The uploaded file name.</param>
        /// <returns>The provider URL of the stored image.</returns>
        Task<string> UploadImageAsync(Stream imageStream, string fileName);
    }

    /// <summary>
    /// Implementation of file management logic, applying business rules.
    /// </summary>
    public class FileService : IFileService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private const int ImageHeaderSize = 12;
        private static ReadOnlySpan<byte> JpegSignature => [0xff, 0xd8, 0xff];
        private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
        private const string InvalidImage = "Solo se admiten fotografías en formato JPG, PNG o WebP.";

        private readonly IStorageService _storageService;
        private readonly ILogger<FileService> _logger;

        public FileService(IStorageService storageService, ILogger<FileService> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<string> UploadImageAsync(Stream imageStream, string fileName)
        {
            if (imageStream.Length > MaxFileSize)
            {
                throw new ArgumentException(ErrorMessages.FileTooLarge);
            }

            string normalizedName = Path.GetFileName(fileName.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new ArgumentException(InvalidImage, nameof(fileName));
            }

            var header = new byte[ImageHeaderSize];
            long position = imageStream.Position;
            int bytesRead = await imageStream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            imageStream.Position = position;

            if (!IsSupportedImage(header.AsSpan(0, bytesRead), Path.GetExtension(normalizedName)))
            {
                throw new ArgumentException(InvalidImage, nameof(fileName));
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Imagen aprobada para almacenamiento. Tamaño: {Size} bytes.",
                    imageStream.Length);
            }

            return await _storageService.UploadFileAsync(imageStream, normalizedName);
        }

        private static bool IsSupportedImage(ReadOnlySpan<byte> header, string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => header.StartsWith(JpegSignature),
                ".png" => header.StartsWith(PngSignature),
                ".webp" => header.Length >= ImageHeaderSize && header.StartsWith("RIFF"u8)
                    && header[8..].StartsWith("WEBP"u8),
                _ => false,
            };
        }
    }
}
