using System.IO;
using System.Threading.Tasks;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for cloud storage operations, keeping the domain agnostic of the specific provider.
    /// </summary>
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName);
    }
}
