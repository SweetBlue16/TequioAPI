namespace Tequio.Domain.Dtos
{
    public class ProductPageDto
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int ItemCount { get; set; }
        public required IEnumerable<ProductDto> Products { get; set; }
    }
}