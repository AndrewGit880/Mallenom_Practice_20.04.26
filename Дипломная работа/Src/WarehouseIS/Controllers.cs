using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WarehouseManagement;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly WarehouseService _service;

    public AuthController(WarehouseService service)
    {
        _service = service;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _service.Authenticate(request.Username, request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "Неверный логин или пароль" });
        }

        return Ok(new { user.Id, user.Username, user.FullName, user.Role });
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly DataContext _context;
    private readonly WarehouseService _service;

    public ProductsController(DataContext context, WarehouseService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _service.GetProducts();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        var existing = await _context.Products.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = product.Name;
        existing.Category = product.Category;
        existing.Price = product.Price;
        existing.MinStock = product.MinStock;
        existing.Unit = product.Unit;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();

        product.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok();
    }
}

[ApiController]
[Route("api/[controller]")]
public class IncomingController : ControllerBase
{
    private readonly DataContext _context;
    private readonly WarehouseService _service;

    public IncomingController(DataContext context, WarehouseService service)
    {
        _context = context;
        _service = service;
    }

    [HttpPost("receive")]
    public async Task<IActionResult> ReceiveGoods([FromBody] ReceiveRequest request)
    {
        var invoice = new IncomingInvoice
        {
            Number = $"IN-{DateTime.Now:yyyyMMddHHmmss}",
            Date = DateTime.UtcNow,
            Supplier = request.Supplier,
            CreatedBy = request.UserId,
            Status = "Черновик"
        };

        var items = request.Items.Select(i => new IncomingItem
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Price = i.Price
        }).ToList();

        var result = await _service.ReceiveGoods(invoice, items);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _context.IncomingInvoices
            .Include(i => i.Items)
            .ThenInclude(it => it.Product)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _context.IncomingInvoices
            .Include(i => i.Items)
            .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();
        return Ok(invoice);
    }
}

[ApiController]
[Route("api/[controller]")]
public class OutgoingController : ControllerBase
{
    private readonly DataContext _context;
    private readonly WarehouseService _service;

    public OutgoingController(DataContext context, WarehouseService service)
    {
        _context = context;
        _service = service;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var invoice = new OutgoingInvoice
        {
            Number = $"OUT-{DateTime.Now:yyyyMMddHHmmss}",
            Date = DateTime.UtcNow,
            Customer = request.Customer,
            CreatedBy = request.UserId,
            Status = "Новый"
        };

        await _context.OutgoingInvoices.AddAsync(invoice);
        await _context.SaveChangesAsync();

        foreach (var item in request.Items)
        {
            _context.OutgoingItems.Add(new OutgoingItem
            {
                InvoiceId = invoice.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PickedQuantity = 0
            });
        }

        await _context.SaveChangesAsync();
        return Ok(invoice);
    }

    [HttpPost("{id}/pick")]
    public async Task<IActionResult> PickOrder(int id)
    {
        var result = await _service.PickOrder(id);
        if (result == null)
        {
            return BadRequest(new { message = "Недостаточно товара на складе" });
        }
        return Ok(result);
    }

    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipOrder(int id)
    {
        var result = await _service.ShipOrder(id);
        if (result == null)
        {
            return BadRequest(new { message = "Заказ не готов к отгрузке" });
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _context.OutgoingInvoices
            .Include(i => i.Items)
            .ThenInclude(it => it.Product)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _context.OutgoingInvoices
            .Include(i => i.Items)
            .ThenInclude(it => it.Product)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null) return NotFound();
        return Ok(invoice);
    }
}

[ApiController]
[Route("api/[controller]")]
public class StatisticsController : ControllerBase
{
    private readonly WarehouseService _service;

    public StatisticsController(WarehouseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatistics()
    {
        var stats = await _service.GetStatistics();
        return Ok(stats);
    }
}

[ApiController]
[Route("api/[controller]")]
public class CellsController : ControllerBase
{
    private readonly DataContext _context;

    public CellsController(DataContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var cells = await _context.WarehouseCells.ToListAsync();
        return Ok(cells);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cell = await _context.WarehouseCells.FindAsync(id);
        if (cell == null) return NotFound();
        return Ok(cell);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ReceiveRequest
{
    public string Supplier { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<ReceiveItem> Items { get; set; } = new List<ReceiveItem>();
}

public class ReceiveItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class CreateOrderRequest
{
    public string Customer { get; set; } = string.Empty;
    public int UserId { get; set; }
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}