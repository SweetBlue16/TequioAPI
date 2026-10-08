using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tequio.Domain.Services;

namespace Tequio.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IStorageService using Cloudinary as the cloud provider.
    /// </summary>
    public class CloudinaryStorageService : IStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryStorageService> _logger;

        public CloudinaryStorageService(IConfiguration configuration, ILogger<CloudinaryStorageService> logger)
        {
            _logger = logger;
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName)
        {
            _logger.LogInformation("Iniciando conexión con Cloudinary para subir el archivo: {FileName}", fileName);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = "tequio-profiles",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException(uploadResult.Error.Message);
            }

            _logger.LogInformation("Archivo {FileName} almacenado exitosamente en Cloudinary. URL generada.", fileName);
            return uploadResult.SecureUrl.ToString();
        }
    }
}
