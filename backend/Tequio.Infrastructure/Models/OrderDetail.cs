using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class OrderDetail
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int PackageId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Package Package { get; set; } = null!;
}
