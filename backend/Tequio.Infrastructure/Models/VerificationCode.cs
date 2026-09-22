using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class VerificationCode
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public DateTime CreationDate { get; set; }

    public DateTime ExpirationDate { get; set; }

    public int RemainingAttempts { get; set; }

    public virtual User User { get; set; } = null!;
}
