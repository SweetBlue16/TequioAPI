using Tequio.Domain.Dtos;

namespace Tequio.Domain.Repositories
{
    /// <summary>
    /// Contract for database operations related to products
    /// </summary>
    public interface IProductRepository
    {
        Task<ProductPageDto> GetProductsFromProducerAsync(int producerId, int pageIndex, int pageSize);
        Task<ProductPageDto> GetProductsByCategoryAsync(int categoryId, int pageIndex, int pageSize);
    }
}