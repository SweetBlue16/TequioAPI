using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class DeliveryPoint
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Street { get; set; } = null!;

    public string Number { get; set; } = null!;

    public string City { get; set; } = null!;

    public string ZipCode { get; set; } = null!;

    public string Neighborhood { get; set; } = null!;

    public string? References { get; set; }

    public string BusinessHours { get; set; } = null!;

    public decimal Latitude { get; set; }

    public decimal Longitude { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();
}
