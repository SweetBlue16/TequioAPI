using System;
using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos
{
    /// <summary>
    /// Data Transfer Object for user registration requests.
    /// </summary>
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string PaternalLastName { get; set; } = string.Empty;

        public string? MaternalLastName { get; set; }

        [Required]
        public DateTime BirthDate { get; set; }

        public string? PhoneNumber { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}
