using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class User
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string PaternalLastName { get; set; } = null!;

    public string? MaternalLastName { get; set; }

    public DateOnly BirthDate { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? Locality { get; set; }

    public string? Biography { get; set; }

    public bool IsVerified { get; set; }

    public DateTime RegistrationDate { get; set; }

    public virtual ICollection<BaseProduct> BaseProducts { get; set; } = new List<BaseProduct>();

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<VerificationCode> VerificationCodes { get; set; } = new List<VerificationCode>();
}
