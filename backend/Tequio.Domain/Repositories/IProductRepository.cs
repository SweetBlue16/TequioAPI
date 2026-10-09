using Tequio.Domain.Dtos;

namespace Tequio.Domain.Repositories
{
    /// <summary>
    /// Contract for database operations related to products
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Persists a new base product record invoking the stored procedure USP_CreateBaseProduct.
        /// </summary>
        /// <param name="producerId">Unique identifier of the authenticated producer.</param>
        /// <param name="dto">Data transfer object containing the base product specifications.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>The generated identifier of the newly created base product.</returns>
        Task<int> CreateBaseProductAsync(
            int producerId,
            CreateBaseProductDto dto,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes logical deactivation of a base product invoking the stored procedure USP_DeactivateBaseProduct.
        /// </summary>
        /// <param name="productId">Unique identifier of the base product to deactivate.</param>
        /// <param name="producerId">Unique identifier of the requesting producer.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>A task representing the asynchronous deactivation operation.</returns>
        Task DeactivateBaseProductAsync(
            int productId,
            int producerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of base products registered by a specific producer.
        /// </summary>
        /// <param name="producerId">Unique identifier of the target producer.</param>
        /// <param name="pageIndex">Zero-based index of the requested page.</param>
        /// <param name="pageSize">Maximum quantity of items per page.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>Paginated response containing matching products and total item count.</returns>
        Task<ProductPageDto> GetProductsFromProducerAsync(
            int producerId, 
            int pageIndex, 
            int pageSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a paginated list of active base products associated with a specific category.
        /// </summary>
        /// <param name="categoryId">Unique identifier of the target category.</param>
        /// <param name="pageIndex">Zero-based index of the requested page.</param>
        /// <param name="pageSize">Maximum quantity of items per page.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
        /// <returns>Paginated response containing matching products and total item count.</returns>
        Task<ProductPageDto> GetProductsByCategoryAsync(
            int categoryId, 
            int pageIndex, 
            int pageSize,
            CancellationToken cancellationToken = default);
    }
}