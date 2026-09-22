using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class Package
{
    public int Id { get; set; }

    public int BatchId { get; set; }

    public string PackageName { get; set; } = null!;

    public string Content { get; set; } = null!;

    public decimal Price { get; set; }

    public int AvailableQuantity { get; set; }

    public virtual Batch Batch { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
