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
        Console.WriteLine($"Попытка входа: {username}");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            Console.WriteLine($"Пользователь {username} не найден");
            return null;
        }

        Console.WriteLine($"Пользователь {username} найден, пропускаем без проверки пароля");

        user.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<List<Product>> GetProducts()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product?> GetProductBySku(string sku)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.SKU == sku);
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

                    var cell = await _context.WarehouseCells
                        .FirstOrDefaultAsync(c => c.Zone == "Хранение" && c.CurrentOccupancy < c.MaxCapacity);

                    if (cell != null)
                    {
                        var stock = await _context.Stocks
                            .FirstOrDefaultAsync(s => s.ProductId == item.ProductId && s.CellId == cell.Id);

                        if (stock == null)
                        {
                            stock = new Stock { ProductId = item.ProductId, CellId = cell.Id, Quantity = 0 };
                            await _context.Stocks.AddAsync(stock);
                        }

                        stock.Quantity += item.Quantity;
                        stock.LastUpdated = DateTime.UtcNow;
                        cell.CurrentOccupancy += item.Quantity;
                    }
                }
            }

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

    public async Task<OutgoingInvoice?> PickOrder(int invoiceId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var invoice = await _context.OutgoingInvoices
                .Include(i => i.Items)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null) return null;

            bool allAvailable = true;
            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || product.CurrentStock < item.Quantity)
                {
                    allAvailable = false;
                    break;
                }
            }

            if (!allAvailable) return null;

            foreach (var item in invoice.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.CurrentStock -= item.Quantity;
                    item.PickedQuantity = item.Quantity;

                    var stockItem = await _context.Stocks
                        .FirstOrDefaultAsync(s => s.ProductId == item.ProductId);

                    if (stockItem != null)
                    {
                        stockItem.Quantity -= item.Quantity;
                        stockItem.LastUpdated = DateTime.UtcNow;

                        var cell = await _context.WarehouseCells.FindAsync(stockItem.CellId);
                        if (cell != null) cell.CurrentOccupancy -= item.Quantity;
                    }
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
        var products = await _context.Products.ToListAsync();
        var cells = await _context.WarehouseCells.ToListAsync();
        var incoming = await _context.IncomingInvoices
            .Where(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();
        var outgoing = await _context.OutgoingInvoices
            .Where(i => i.CreatedAt >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        return new
        {
            TotalProducts = products.Count,
            TotalStock = products.Sum(p => p.CurrentStock),
            TotalValue = products.Sum(p => p.CurrentStock * p.Price),
            WarehouseOccupancy = cells.Sum(c => c.CurrentOccupancy),
            WarehouseCapacity = cells.Sum(c => c.MaxCapacity),
            IncomingLastMonth = incoming,
            OutgoingLastMonth = outgoing,
            LowStockProducts = products.Where(p => p.CurrentStock <= p.MinStock).Select(p => new { p.SKU, p.Name, p.CurrentStock, p.MinStock })
        };
    }
}