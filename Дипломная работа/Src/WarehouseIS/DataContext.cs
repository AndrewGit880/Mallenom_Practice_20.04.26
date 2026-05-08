using Microsoft.EntityFrameworkCore;

namespace WarehouseManagement;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<WarehouseCell> WarehouseCells { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<IncomingInvoice> IncomingInvoices { get; set; }
    public DbSet<IncomingItem> IncomingItems { get; set; }
    public DbSet<OutgoingInvoice> OutgoingInvoices { get; set; }
    public DbSet<OutgoingItem> OutgoingItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Stock>()
            .HasOne(s => s.Product)
            .WithMany(p => p.Stocks)
            .HasForeignKey(s => s.ProductId);

        modelBuilder.Entity<Stock>()
            .HasOne(s => s.Cell)
            .WithMany()
            .HasForeignKey(s => s.CellId);
    }
}