using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagement;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Role { get; set; } = "Кладовщик";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
}

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = "шт";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public int MinStock { get; set; } = 0;
    public int CurrentStock { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}

public class WarehouseCell
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Zone { get; set; } = string.Empty;

    public int MaxCapacity { get; set; } = 1000;
    public int CurrentOccupancy { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class Stock
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }
    public int CellId { get; set; }
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }

    [ForeignKey("CellId")]
    public virtual WarehouseCell? Cell { get; set; }
}

public class IncomingInvoice
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string Supplier { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "Черновик";

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<IncomingItem> Items { get; set; } = new List<IncomingItem>();
}

public class IncomingItem
{
    [Key]
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [ForeignKey("InvoiceId")]
    public virtual IncomingInvoice? Invoice { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}

public class OutgoingInvoice
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string Customer { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Status { get; set; } = "Новый";

    public int CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<OutgoingItem> Items { get; set; } = new List<OutgoingItem>();
}

public class OutgoingItem
{
    [Key]
    public int Id { get; set; }

    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public int PickedQuantity { get; set; } = 0;

    [ForeignKey("InvoiceId")]
    public virtual OutgoingInvoice? Invoice { get; set; }

    [ForeignKey("ProductId")]
    public virtual Product? Product { get; set; }
}