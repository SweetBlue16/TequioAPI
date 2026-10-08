namespace Tequio.Domain.Dtos
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string PaternalLastName { get; set; } = string.Empty;
        public string? MaternalLastName { get; set; }
        public DateTime BirthDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Locality { get; set; }
        public string? Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsVerified { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}
