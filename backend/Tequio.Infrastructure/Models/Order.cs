using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class Order
{
    public int Id { get; set; }

    public int BuyerId { get; set; }

    public int BatchId { get; set; }

    public DateTime TransactionDate { get; set; }

    public decimal TotalPaid { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public string? PaymentReference { get; set; }

    public string? DeliveryCode { get; set; }

    public string DeliveryStatus { get; set; } = null!;

    public DateTime? DeliveryValidationDate { get; set; }

    public virtual Batch Batch { get; set; } = null!;

    public virtual User Buyer { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
