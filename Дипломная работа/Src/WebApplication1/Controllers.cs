using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace WarehouseManagement;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DataContext _context;
    private readonly WarehouseService _service;

    public AuthController(DataContext context, WarehouseService service)
    {
        _context = context;
        _service = service;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _service.Authenticate(request.Username, request.Password);
        if (user == null) return Unauthorized(new { message = "Неверный логин или пароль" });

        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("Username", user.Username);
        HttpContext.Session.SetString("UserRole", user.Role);
        HttpContext.Session.SetString("UserFullName", user.FullName);

        return Ok(new { user.Id, user.Username, user.FullName, user.Role });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Выход выполнен" });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        return Ok(new
        {
            Id = int.Parse(userId),
            Username = HttpContext.Session.GetString("Username"),
            FullName = HttpContext.Session.GetString("UserFullName"),
            Role = HttpContext.Session.GetString("UserRole")
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (existingUser != null) return BadRequest(new { message = "Пользователь уже существует" });

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = request.Role ?? "Кладовщик",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { user.Id, user.Username, user.FullName, user.Role });
    }
}

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly DataContext _context;

    public AdminController(DataContext context)
    {
        _context = context;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => new { u.Id, u.Username, u.FullName, u.Role })
            .ToListAsync();
        return Ok(users);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly DataContext _context;

    public ProductsController(DataContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.SKU, p.Name, p.Category, p.Price, p.MinStock, p.CurrentStock })
            .ToListAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.CurrentStock = 0;
        product.IsActive = true;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return Ok(new { product.Id, product.SKU, product.Name, product.Price, product.MinStock });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return NotFound();
        _context.Products.Remove(product);
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
            Status = "Оприходована"
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
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.Id, i.Number, i.Date, i.Supplier, i.Status })
            .ToListAsync();
        return Ok(invoices);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await _context.IncomingInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return NotFound();

        foreach (var item in invoice.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.CurrentStock -= item.Quantity;
            }
        }

        _context.IncomingInvoices.Remove(invoice);
        await _context.SaveChangesAsync();
        return Ok();
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
        return Ok(new { invoice.Id, invoice.Number, invoice.Customer, invoice.Status });
    }

    [HttpPost("{id}/pick")]
    public async Task<IActionResult> PickOrder(int id)
    {
        var result = await _service.PickOrder(id);
        if (result == null) return BadRequest(new { message = "Недостаточно товара" });
        return Ok(result);
    }

    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipOrder(int id)
    {
        var result = await _service.ShipOrder(id);
        if (result == null) return BadRequest(new { message = "Заказ не готов" });
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _context.OutgoingInvoices
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new { i.Id, i.Number, i.Date, i.Customer, i.Status })
            .ToListAsync();
        return Ok(invoices);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var invoice = await _context.OutgoingInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id);
        if (invoice == null) return NotFound();

        foreach (var item in invoice.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.CurrentStock += item.Quantity;
            }
        }

        _context.OutgoingInvoices.Remove(invoice);
        await _context.SaveChangesAsync();
        return Ok();
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
        var cells = await _context.WarehouseCells
            .Where(c => c.IsActive)
            .Select(c => new { c.Id, c.Code, c.Zone, c.MaxCapacity, c.CurrentOccupancy })
            .ToListAsync();
        return Ok(cells);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] WarehouseCell cell)
    {
        cell.CurrentOccupancy = 0;
        cell.IsActive = true;
        _context.WarehouseCells.Add(cell);
        await _context.SaveChangesAsync();
        return Ok(cell);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cell = await _context.WarehouseCells.FindAsync(id);
        if (cell == null) return NotFound();
        cell.IsActive = false;
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class LoginRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
public class RegisterRequest { public string Username { get; set; } = ""; public string Password { get; set; } = ""; public string FullName { get; set; } = ""; public string? Role { get; set; } }
public class ReceiveRequest { public string Supplier { get; set; } = ""; public int UserId { get; set; } public List<ReceiveItem> Items { get; set; } = new(); }
public class ReceiveItem { public int ProductId { get; set; } public int Quantity { get; set; } public decimal Price { get; set; } }
public class CreateOrderRequest { public string Customer { get; set; } = ""; public int UserId { get; set; } public List<OrderItem> Items { get; set; } = new(); }
public class OrderItem { public int ProductId { get; set; } public int Quantity { get; set; } }