namespace Tequio.Domain.Dtos
{
    public class ProductDto
    {
        public int Id { get; set; }
        
        public int ProducerId { get; set; }
        
        public int CategoryId { get; set; }
        
        public string Name { get; set; } = null!;
        
        public string ShortDescription { get; set; } =  null!;
        
        public string? ImageUrl { get; set; }
        
        public string MeasurementUnit { get; set; } = null!;
        
        public bool IsActive { get; set; }
    }
}
