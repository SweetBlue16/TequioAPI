using System.ComponentModel.DataAnnotations;
using Tequio.Domain.Constants;

namespace Tequio.Domain.Dtos
{
    public class UpdateProfilePictureDto
    {
        [Required(ErrorMessage = ErrorMessages.RequiredImageUrl)]
        [Url(ErrorMessage = ErrorMessages.InvalidUrl)]
        public string PictureUrl { get; set; } = string.Empty;
    }
}
