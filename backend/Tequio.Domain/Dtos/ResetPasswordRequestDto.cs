using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos;

/// <summary>
/// Data transfer object for resetting a user password using a recovery verification code.
/// </summary>
public class ResetPasswordRequestDto
{
    /// <summary>
    /// User email address.
    /// </summary>
    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
    [StringLength(80, ErrorMessage = "El correo no debe exceder los 80 caracteres.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Six-digit verification code.
    /// </summary>
    [Required(ErrorMessage = "El código de verificación es obligatorio.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "El código debe contener exactamente 6 dígitos.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// New password for the user.
    /// </summary>
    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}