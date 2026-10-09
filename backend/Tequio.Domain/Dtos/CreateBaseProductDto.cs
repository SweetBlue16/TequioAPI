using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos;

/// <summary>
/// Data transfer object for creating a new base catalog product.
/// </summary>
public class CreateBaseProductDto
{
    [Required(ErrorMessage = "La categoría es obligatoria.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de categoría no es válido.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción corta es obligatoria.")]
    [StringLength(255, MinimumLength = 5, ErrorMessage = "La descripción no debe exceder 255 caracteres.")]
    public string ShortDescription { get; set; } = string.Empty;

    [Url(ErrorMessage = "La URL de la imagen no tiene un formato válido.")]
    [StringLength(500, ErrorMessage = "La URL no puede exceder 500 caracteres.")]
    public string? ImageUrl { get; set; }

    [Required(ErrorMessage = "La unidad de medida es obligatoria.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "La unidad de medida no debe exceder 50 caracteres.")]
    public string MeasurementUnit { get; set; } = string.Empty;
}