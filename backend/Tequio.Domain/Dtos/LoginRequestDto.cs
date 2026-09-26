using System;
using System.ComponentModel.DataAnnotations;

namespace Tequio.Domain.Dtos
{
    /// <summary>
    /// Data Transfer Object for user login requests.
    /// </summary>
    public class LoginRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
