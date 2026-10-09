using Microsoft.Extensions.Logging;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract for product creation, deactivation and retrieval
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Stub
        /// </summary>
        /// <returns></returns>
        Task<int> CreateProductAsync();
        
        /// <summary>
        /// Stub
        /// </summary>
        /// <returns></returns>
        Task<int> DeactivateProductAsync();

        Task<ProductPageDto> GetProductsFromProducer(int producerId, int pageIndex, int pageSize);
        
        Task<ProductPageDto> GetProductsByCategory(int categoryId, int pageIndex, int pageSize);
    }

    /// <summary>
    /// Implementation of IProductService
    /// </summary>
    public class ProductService(IProductRepository repository, ILogger<ProductService> logger)
        : IProductService
    {

        public async Task<int> CreateProductAsync()
        {
            logger.LogInformation("Creating product... COMPLETE ME");
            return 0;
        }

        public async Task<int> DeactivateProductAsync()
        {
            logger.LogInformation("Deactivating product... COMPLETE ME");
            return 0;
        }

        public async Task<ProductPageDto> GetProductsFromProducer(int producerId, int pageIndex, int pageSize)
        {
            return await repository.GetProductsFromProducerAsync(producerId, pageIndex, pageSize);
        }

        public async Task<ProductPageDto> GetProductsByCategory(int categoryId, int pageIndex, int pageSize)
        {
            return await repository.GetProductsByCategoryAsync(categoryId, pageIndex, pageSize);
        }
    }
}
