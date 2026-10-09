using Microsoft.Extensions.Logging;
using Tequio.Domain.Constants;
using Tequio.Domain.Dtos;
using Tequio.Domain.Repositories;

namespace Tequio.Domain.Services
{
    /// <summary>
    /// Contract defining business logic operations for base product catalog management and retrieval.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Registers a new base product within the catalog on behalf of an authenticated producer.
        /// </summary>
        /// <param name="producerId">Unique identifier of the authenticated producer.</param>
        /// <param name="dto">Data transfer object containing the base product specifications.</param>
        /// <returns>The generated identifier of the newly created base product.</returns>
        Task<int> CreateBaseProductAsync(int producerId, CreateBaseProductDto dto);

        /// <summary>
        /// Logically deactivates an existing base product belonging to the specified producer.
        /// </summary>
        /// <param name="productId">Unique identifier of the base product to deactivate.</param>
        /// <param name="producerId">Unique identifier of the requesting producer.</param>
        /// <returns>A task representing the asynchronous deactivation process.</returns>
        Task DeactivateBaseProductAsync(int productId, int producerId);

        /// <summary>
        /// Retrieves a paginated list of base products registered by a specific producer.
        /// </summary>
        /// <param name="producerId">Unique identifier of the target producer.</param>
        /// <param name="pageIndex">Zero-based index of the requested page.</param>
        /// <param name="pageSize">Maximum quantity of items per page.</param>
        /// <returns>Paginated response containing matching products and total item count.</returns>
        Task<ProductPageDto> GetProductsFromProducer(int producerId, int pageIndex, int pageSize);

        /// <summary>
        /// Retrieves a paginated list of active base products associated with a specific category.
        /// </summary>
        /// <param name="categoryId">Unique identifier of the target category.</param>
        /// <param name="pageIndex">Zero-based index of the requested page.</param>
        /// <param name="pageSize">Maximum quantity of items per page.</param>
        /// <returns>Paginated response containing matching products and total item count.</returns>
        Task<ProductPageDto> GetProductsByCategory(int categoryId, int pageIndex, int pageSize);
    }

    /// <summary>
    /// Implements catalog business workflows and coordinates persistence with repository layer.
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;
        private readonly ILogger<ProductService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductService"/> class.
        /// </summary>
        /// <param name="repository">Underlying repository executing database procedures.</param>
        /// <param name="logger">Diagnostic logging component.</param>
        public ProductService(IProductRepository repository, ILogger<ProductService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<int> CreateBaseProductAsync(int producerId, CreateBaseProductDto dto)
        {
            if (producerId <= 0)
            {
                throw new ArgumentException(ErrorMessages.ProducerNotFoundOrUnauthorized);
            }

            if (dto.CategoryId <= 0)
            {
                throw new ArgumentException(ErrorMessages.CategoryNotFound);
            }

            dto.Name = dto.Name?.Trim() ?? string.Empty;
            dto.ShortDescription = dto.ShortDescription?.Trim() ?? string.Empty;
            dto.MeasurementUnit = dto.MeasurementUnit?.Trim() ?? string.Empty;
            dto.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();

            _logger.LogInformation(
                "Iniciando creación de producto '{ProductName}' para el productor {ProducerId}.",
                dto.Name,
                producerId);

            int newProductId = await _repository.CreateBaseProductAsync(producerId, dto);

            _logger.LogInformation(
                "Producto '{ProductName}' creado exitosamente con ID {ProductId} para el productor {ProducerId}.",
                dto.Name,
                newProductId,
                producerId);

            return newProductId;
        }

        /// <inheritdoc />
        public async Task DeactivateBaseProductAsync(int productId, int producerId)
        {
            if (productId <= 0)
            {
                throw new ArgumentException(ErrorMessages.RecordNotFound);
            }

            if (producerId <= 0)
            {
                throw new ArgumentException(ErrorMessages.ProducerNotFoundOrUnauthorized);
            }

            _logger.LogInformation(
                "Iniciando desactivación del producto {ProductId} para el productor {ProducerId}.",
                productId,
                producerId);

            await _repository.DeactivateBaseProductAsync(productId, producerId);

            _logger.LogInformation(
                "Producto {ProductId} desactivado exitosamente por el productor {ProducerId}.",
                productId,
                producerId);
        }

        /// <inheritdoc />
        public async Task<ProductPageDto> GetProductsFromProducer(int producerId, int pageIndex, int pageSize)
        {
            return await _repository.GetProductsFromProducerAsync(producerId, pageIndex, pageSize);
        }

        /// <inheritdoc />
        public async Task<ProductPageDto> GetProductsByCategory(int categoryId, int pageIndex, int pageSize)
        {
            return await _repository.GetProductsByCategoryAsync(categoryId, pageIndex, pageSize);
        }
    }
}