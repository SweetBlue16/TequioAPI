using Microsoft.AspNetCore.Mvc;
using Tequio.Api.Validators;
using Tequio.Domain.Services;

namespace Tequio.Api.Controllers
{
    /// <summary>
    /// Manages product creation, deactivation and retrieval
    /// </summary>
    [ApiController]
    [Route("api/v1/products")]
    public class ProductController(IProductService productService, ILogger<ProductController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Retrieves products belonging to the specified user
        /// </summary>
        /// <param name="producerId">The userId of the producer</param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        [HttpGet("producers/{producerId}")]
        public async Task<IActionResult> GetProductsFromProducerAsync([FromRoute] string producerId, [FromQuery] string pageIndex, [FromQuery] string pageSize)
        {
            var id = StringConverter.ToInt(producerId, "producerId");
            var index = StringConverter.ToInt(pageIndex, "pageIndex");
            var size = StringConverter.ToInt(pageSize, "pageSize");
            
            NumberValidator.ValidateGreaterOrEqualToZero(id, "producerId");
            PageValidator.ValidatePageArguments(index, size);
            index = index - 1;

            var page = await productService.GetProductsFromProducer(id, index, size);
            return Ok(page);
        }

        /// <summary>
        /// Retrieves products belonging to the specified category
        /// </summary>
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategoryAsync([FromRoute] string categoryId, [FromQuery] string pageIndex, [FromQuery] string pageSize)
        {
            var id = StringConverter.ToInt(categoryId, "categoryId");
            var index = StringConverter.ToInt(pageIndex, "pageIndex");
            var size = StringConverter.ToInt(pageSize, "pageSize");
            
            NumberValidator.ValidateGreaterOrEqualToZero(id, "categoryId");
            PageValidator.ValidatePageArguments(index, size);
            index = index - 1;
            
            var page = await productService.GetProductsByCategory(id, index, size);
            return Ok(page);
        }
    }
}