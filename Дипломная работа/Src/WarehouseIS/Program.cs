using Microsoft.EntityFrameworkCore;
using WarehouseManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseStaticFiles();
app.MapControllers();

app.MapGet("/", context =>
{
    context.Response.Redirect("/index.html");
    return Task.CompletedTask;
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
    try
    {
        context.Database.EnsureCreated();
        Console.WriteLine("База данных PostgreSQL создана/подключена успешно");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка подключения к PostgreSQL: {ex.Message}");
        Console.WriteLine("Проверьте: 1) Запущен ли PostgreSQL сервер 2) Правильный пароль в appsettings.json");
    }

    DbInitializer.Initialize(context);
}

app.Run();