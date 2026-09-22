using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class BaseProduct
{
    public int Id { get; set; }

    public int ProducerId { get; set; }

    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string ShortDescription { get; set; } = null!;

    public string? ImageUrl { get; set; }

    public string MeasurementUnit { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Batch> Batches { get; set; } = new List<Batch>();

    public virtual ProductCategory Category { get; set; } = null!;

    public virtual User Producer { get; set; } = null!;
}
