using System;
using System.Collections.Generic;

namespace Tequio.Infrastructure.Models;

public partial class ProductCategory
{
    public int Id { get; set; }

    public int? ParentCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<BaseProduct> BaseProducts { get; set; } = new List<BaseProduct>();

    public virtual ICollection<ProductCategory> InverseParentCategory { get; set; } = new List<ProductCategory>();

    public virtual ProductCategory? ParentCategory { get; set; }
}
