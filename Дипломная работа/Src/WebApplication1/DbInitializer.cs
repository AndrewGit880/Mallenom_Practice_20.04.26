namespace WarehouseManagement;

public static class DbInitializer
{
    public static void Initialize(DataContext context)
    {
        Console.WriteLine("=== ИНИЦИАЛИЗАЦИЯ БД ===");

        try
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                Console.WriteLine("Добавляем тестовых пользователей...");

                var users = new User[]
                {
                    new User {
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                        FullName = "Администратор",
                        Role = "Администратор",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User {
                        Username = "manager",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123"),
                        FullName = "Менеджер склада",
                        Role = "Менеджер",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new User {
                        Username = "clerk",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("clerk123"),
                        FullName = "Кладовщик",
                        Role = "Кладовщик",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
                Console.WriteLine($"Добавлено {users.Length} пользователей");
            }

            if (!context.WarehouseCells.Any())
            {
                Console.WriteLine("Добавляем тестовые ячейки...");
                var cells = new WarehouseCell[]
                {
                    new WarehouseCell { Code = "A-01", Zone = "Приемка", MaxCapacity = 500, CurrentOccupancy = 0, IsActive = true },
                    new WarehouseCell { Code = "A-02", Zone = "Приемка", MaxCapacity = 500, CurrentOccupancy = 0, IsActive = true },
                    new WarehouseCell { Code = "B-01", Zone = "Хранение", MaxCapacity = 1000, CurrentOccupancy = 0, IsActive = true },
                    new WarehouseCell { Code = "B-02", Zone = "Хранение", MaxCapacity = 1000, CurrentOccupancy = 0, IsActive = true },
                    new WarehouseCell { Code = "C-01", Zone = "Отгрузка", MaxCapacity = 500, CurrentOccupancy = 0, IsActive = true }
                };
                context.WarehouseCells.AddRange(cells);
                context.SaveChanges();
                Console.WriteLine($"Добавлено {cells.Length} ячеек");
            }

            if (!context.Products.Any())
            {
                Console.WriteLine("Добавляем тестовые товары...");
                var products = new Product[]
                {
                    new Product { SKU = "PRD-001", Name = "Ноутбук Lenovo", Category = "Электроника", Unit = "шт", Price = 45000, MinStock = 5, CurrentStock = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Product { SKU = "PRD-002", Name = "Мышь компьютерная", Category = "Электроника", Unit = "шт", Price = 800, MinStock = 20, CurrentStock = 0, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Product { SKU = "PRD-003", Name = "Клавиатура", Category = "Электроника", Unit = "шт", Price = 2500, MinStock = 10, CurrentStock = 0, IsActive = true, CreatedAt = DateTime.UtcNow }
                };
                context.Products.AddRange(products);
                context.SaveChanges();
                Console.WriteLine($"Добавлено {products.Length} товаров");
            }

            Console.WriteLine("Инициализация БД завершена успешно!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}