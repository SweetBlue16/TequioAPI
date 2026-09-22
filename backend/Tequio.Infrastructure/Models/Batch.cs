using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class Batch
{
    public int Id { get; set; }

    public int BaseProductId { get; set; }

    public int ProducerId { get; set; }

    public int DeliveryPointId { get; set; }

    public string BatchName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int MinimumGoal { get; set; }

    public DateTime Deadline { get; set; }

    public string Status { get; set; } = null!;

    public DateTime PublicationDate { get; set; }

    public virtual BaseProduct BaseProduct { get; set; } = null!;

    public virtual DeliveryPoint DeliveryPoint { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Package> Packages { get; set; } = new List<Package>();

    public virtual User Producer { get; set; } = null!;
}
