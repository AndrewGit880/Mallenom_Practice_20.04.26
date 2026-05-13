using Microsoft.EntityFrameworkCore;

namespace WarehouseManagement;

public class WarehouseService
{
    private readonly DataContext _context;

    public WarehouseService(DataContext context)
    {
        _context = context;
    }

    public async Task<User?> Authenticate(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<List<Product>> GetProducts()
    {
        return await _context.Products.Where(p => p.IsActive).ToListAsync();
    }

    public async Task<IncomingInvoice?> ReceiveGoods(IncomingInvoice invoice, List<IncomingItem> items)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.Status = "Оприходована";
            await _context.IncomingInvoices.AddAsync(invoice);
            await _context.SaveChangesAsync();

            foreach (var item in items)
            {
                item.InvoiceId = invoice.Id;
                await _context.IncomingItems.AddAsync(item);

                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock += item.Quantity;
                    _context.Entry(product).State = EntityState.Modified;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return invoice;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Ошибка: {ex.Message}");
            throw;
        }
    }

    public async Task<OutgoingInvoice?> PickOrder(int invoiceId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.OutgoingInvoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return null;

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.CurrentStock < item.Quantity) return null;
            }

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= item.Quantity;
                    item.PickedQuantity = item.Quantity;
                }
            }

            invoice.Status = "Собран";
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return invoice;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<OutgoingInvoice?> ShipOrder(int invoiceId)
    {
        var invoice = await _context.OutgoingInvoices.FindAsync(invoiceId);
        if (invoice == null || invoice.Status != "Собран") return null;
        invoice.Status = "Отгружен";
        await _context.SaveChangesAsync();
        return invoice;
    }

    public async Task<object> GetStatistics()
    {
        var products = await _context.Products.Where(p => p.IsActive).ToListAsync();
        var cells = await _context.WarehouseCells.Where(c => c.IsActive).ToListAsync();
        var incoming = await _context.IncomingInvoices
            .Where(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();
        var outgoing = await _context.OutgoingInvoices
            .Where(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        var totalStock = products.Sum(p => p.CurrentStock);
        var totalCapacity = cells.Where(c => c.Zone == "Хранение").Sum(c => c.MaxCapacity);
        var warehouseOccupancy = totalStock * 2;

        return new
        {
            TotalProducts = products.Count,
            TotalStock = totalStock,
            TotalValue = products.Sum(p => p.CurrentStock * p.Price),
            WarehouseOccupancy = warehouseOccupancy,
            WarehouseCapacity = totalCapacity,
            IncomingLastMonth = incoming,
            OutgoingLastMonth = outgoing,
            LowStockProducts = products.Where(p => p.CurrentStock <= p.MinStock).Select(p => new { p.SKU, p.Name, p.CurrentStock, p.MinStock })
        };
    }
}