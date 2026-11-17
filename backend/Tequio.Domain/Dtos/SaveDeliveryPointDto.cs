using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos;

/// <summary>Editable physical delivery location; null coordinates differ from zero.</summary>
public class SaveDeliveryPointDto
{
    [Required(ErrorMessage = "El campo nombre del punto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El campo nombre del punto permite hasta 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo calle es obligatorio.")]
    [StringLength(150, ErrorMessage = "El campo calle permite hasta 150 caracteres.")]
    public string Street { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo número exterior es obligatorio.")]
    [StringLength(50, ErrorMessage = "El campo número exterior permite hasta 50 caracteres.")]
    public string Number { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo colonia es obligatorio.")]
    [StringLength(100, ErrorMessage = "El campo colonia permite hasta 100 caracteres.")]
    public string Neighborhood { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo código postal es obligatorio.")]
    [StringLength(20, ErrorMessage = "El campo código postal permite hasta 20 caracteres.")]
    public string ZipCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo municipio es obligatorio.")]
    [StringLength(100, ErrorMessage = "El campo municipio permite hasta 100 caracteres.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "El campo horario es obligatorio.")]
    [StringLength(200, ErrorMessage = "El campo horario permite hasta 200 caracteres.")]
    public string BusinessHours { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Las referencias permiten hasta 400 caracteres.")]
    public string? References { get; set; }

    [Required(ErrorMessage = "La latitud es obligatoria.")]
    [Range(typeof(decimal), "-90", "90", ErrorMessage = "La latitud debe estar entre -90 y 90.")]
    public decimal? Latitude { get; set; }

    [Required(ErrorMessage = "La longitud es obligatoria.")]
    [Range(typeof(decimal), "-180", "180", ErrorMessage = "La longitud debe estar entre -180 y 180.")]
    public decimal? Longitude { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecciona un municipio válido.")]
    public int? MunicipalityId { get; set; }
}

/// <summary>Persisted delivery point, including availability.</summary>
public sealed class DeliveryPointDto : SaveDeliveryPointDto
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>One-based administrative catalog page.</summary>
public sealed record DeliveryPointPageDto(
    IReadOnlyList<DeliveryPointDto> Items, int TotalCount, int PageIndex, int PageSize);
