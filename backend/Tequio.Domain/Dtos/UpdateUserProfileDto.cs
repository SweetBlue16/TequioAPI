using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos
{
    public class UpdateUserProfileDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
        [StringLength(100, ErrorMessage = "El apellido paterno no puede exceder los 100 caracteres.")]
        public string PaternalLastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "El apellido materno no puede exceder los 100 caracteres.")]
        public string? MaternalLastName { get; set; }

        [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
        public DateTime BirthDate { get; set; }

        [StringLength(20, ErrorMessage = "El número de teléfono no puede exceder los 20 caracteres.")]
        public string? PhoneNumber { get; set; }

        [StringLength(100, ErrorMessage = "La localidad no puede exceder los 100 caracteres.")]
        public string? Locality { get; set; }

        public string? Biography { get; set; }
    }
}
