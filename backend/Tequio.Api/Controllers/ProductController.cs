using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tequio.Api.Extensions;
using Tequio.Api.Validators;
using Tequio.Domain.Dtos;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Manages product creation, deactivation and retrieval.
    /// </summary>
    [ApiController]
    [Route("api/v1/products")]
    public class ProductController(IProductService productService, ILogger<ProductController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves products belonging to the specified user.
        /// </summary>
        /// <param name="producerId">The userId of the producer.</param>
        /// <param name="pageIndex">Current page zero-based index.</param>
        /// <param name="pageSize">Amount of products per page.</param>
        [HttpGet("producers/{producerId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProductsFromProducerAsync(
            [FromRoute] string producerId,
            [FromQuery] string pageIndex,
            [FromQuery] string pageSize)
        {
            var id = StringConverter.ToInt(producerId, "producerId");
            var index = StringConverter.ToInt(pageIndex, "pageIndex");
            var size = StringConverter.ToInt(pageSize, "pageSize");

            NumberValidator.ValidateGreaterOrEqualToZero(id, "producerId");
            PageValidator.ValidatePageArguments(index, size);

            var page = await productService.GetProductsFromProducer(id, index, size);
            return Ok(page);
        }

        /// <summary>
        /// Retrieves products belonging to the specified category.
        /// </summary>
        /// <param name="categoryId">Target category identifier.</param>
        /// <param name="pageIndex">Current page zero-based index.</param>
        /// <param name="pageSize">Amount of products per page.</param>
        [HttpGet("category/{categoryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProductsByCategoryAsync(
            [FromRoute] string categoryId,
            [FromQuery] string pageIndex,
            [FromQuery] string pageSize)
        {
            var id = StringConverter.ToInt(categoryId, "categoryId");
            var index = StringConverter.ToInt(pageIndex, "pageIndex");
            var size = StringConverter.ToInt(pageSize, "pageSize");

            NumberValidator.ValidateGreaterOrEqualToZero(id, "categoryId");
            PageValidator.ValidatePageArguments(index, size);

            var page = await productService.GetProductsByCategory(id, index, size);
            return Ok(page);
        }

        /// <summary>
        /// Registers a new base product for the authenticated producer.
        /// </summary>
        /// <param name="dto">Product creation parameters payload.</param>
        /// <returns>A created result containing the generated identifier.</returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBaseProductAsync([FromBody] CreateBaseProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int producerId = User.GetUserId();
            logger.LogInformation(
                "El productor {ProducerId} solicitó el registro del producto base '{ProductName}'.",
                producerId,
                dto.Name);

            int createdProductId = await productService.CreateBaseProductAsync(producerId, dto);

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    mensaje = "Producto registrado exitosamente.",
                    productId = createdProductId
                });
        }

        /// <summary>
        /// Logically deactivates a base product verifying active batch restrictions (RN-10).
        /// </summary>
        /// <param name="productId">The target base product identifier.</param>
        /// <returns>Confirmation message of the deactivation.</returns>
        [HttpPatch("{productId}/deactive")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeactivateBaseProductAsync([FromRoute] string productId)
        {
            var id = StringConverter.ToInt(productId, "productId");
            NumberValidator.ValidateGreaterOrEqualToZero(id, "productId");

            int producerId = User.GetUserId();
            logger.LogInformation(
                "El productor {ProducerId} solicitó la desactivación del producto base {ProductId}.",
                producerId,
                id);

            await productService.DeactivateBaseProductAsync(id, producerId);

            return Ok(new
            {
                mensaje = "Producto desactivado exitosamente."
            });
        }
    }
}