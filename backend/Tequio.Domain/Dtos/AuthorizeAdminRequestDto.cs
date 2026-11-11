using System.ComponentModel.DataAnnotations;
using Tequio.Domain.Constants;

namespace Tequio.Domain.Dtos
{
    public class AuthorizeAdminRequestDto
    {
        [Required(ErrorMessage = ErrorMessages.MasterAdminEmailRequired)]
        [EmailAddress(ErrorMessage = ErrorMessages.InvalidEmail)]
        public string NewAdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.OtpRequired)]
        public string VerificationCode { get; set; } = string.Empty;
    }
}