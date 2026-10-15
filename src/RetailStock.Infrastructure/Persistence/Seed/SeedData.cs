using RetailStock.Domain.Enums;

namespace RetailStock.Infrastructure.Persistence.Seed;

/// <summary>
/// Datos semilla que EF Core inserta mediante HasData() dentro de la migración.
/// Equivalente en Spring: data.sql o un changeset de Flyway/Liquibase con INSERTs.
///
/// Reglas de HasData:
///  - Los Ids deben ser fijos (EF los compara entre migraciones para generar INSERT/UPDATE/DELETE).
///  - Los valores deben ser deterministas: nada de DateTime.Now ni hashes calculados en tiempo de ejecución,
///    o EF generaría cambios en cada migración.
///  - Se usan objetos anónimos porque las entidades tienen setters privados.
/// </summary>
internal static class SeedData
{
    // Contraseña: Admin123!  (hash BCrypt precalculado, factor de costo 11). Cámbiala en producción.
    private const string AdminPasswordHash = "$2a$11$2VijrJstQcT869rj49LnmO8nYPgmIDxJtlATEhe8rNrqAWhAIChtW";

    public static readonly object[] Users =
    {
        new { Id = 1, Email = "admin@retailstock.com", PasswordHash = AdminPasswordHash, Role = UserRole.Admin }
    };

    public static readonly object[] Categories =
    {
        new { Id = 1, Name = "Abarrotes" },
        new { Id = 2, Name = "Bebidas" },
        new { Id = 3, Name = "Limpieza" }
    };

    public static readonly object[] Products =
    {
        new { Id = 1,  Code = "ABR-001", Name = "Arroz Flor 2 kg",              CategoryId = 1, Price = 2.85m, IsActive = true },
        new { Id = 2,  Code = "ABR-002", Name = "Azúcar San Carlos 2 kg",       CategoryId = 1, Price = 2.40m, IsActive = true },
        new { Id = 3,  Code = "ABR-003", Name = "Aceite La Favorita 1 L",       CategoryId = 1, Price = 3.15m, IsActive = true },
        new { Id = 4,  Code = "ABR-004", Name = "Atún Real 180 g",              CategoryId = 1, Price = 1.65m, IsActive = true },
        new { Id = 5,  Code = "BEB-001", Name = "Agua Güitig 1.5 L",            CategoryId = 2, Price = 1.10m, IsActive = true },
        new { Id = 6,  Code = "BEB-002", Name = "Coca-Cola 3 L",                CategoryId = 2, Price = 3.25m, IsActive = true },
        new { Id = 7,  Code = "BEB-003", Name = "Leche Vita entera 1 L",        CategoryId = 2, Price = 1.05m, IsActive = true },
        new { Id = 8,  Code = "LIM-001", Name = "Detergente Deja 1 kg",         CategoryId = 3, Price = 3.90m, IsActive = true },
        new { Id = 9,  Code = "LIM-002", Name = "Cloro Clorox 2 L",             CategoryId = 3, Price = 2.20m, IsActive = true },
        new { Id = 10, Code = "LIM-003", Name = "Papel higiénico Familia x12",  CategoryId = 3, Price = 6.75m, IsActive = true }
    };

    public static readonly object[] Warehouses =
    {
        new { Id = 1, Name = "Bodega Central", Location = "Guayaquil - Vía a Daule km 10" },
        new { Id = 2, Name = "Sucursal Norte", Location = "Quito - Av. 6 de Diciembre" }
    };

    // Algunas cantidades bajas a propósito para probar el reporte sp_LowStock en la Fase 6
    public static readonly object[] Stocks =
    {
        new { ProductId = 1,  WarehouseId = 1, Quantity = 500 }, new { ProductId = 1,  WarehouseId = 2, Quantity = 120 },
        new { ProductId = 2,  WarehouseId = 1, Quantity = 400 }, new { ProductId = 2,  WarehouseId = 2, Quantity = 90 },
        new { ProductId = 3,  WarehouseId = 1, Quantity = 300 }, new { ProductId = 3,  WarehouseId = 2, Quantity = 8 },
        new { ProductId = 4,  WarehouseId = 1, Quantity = 600 }, new { ProductId = 4,  WarehouseId = 2, Quantity = 150 },
        new { ProductId = 5,  WarehouseId = 1, Quantity = 800 }, new { ProductId = 5,  WarehouseId = 2, Quantity = 200 },
        new { ProductId = 6,  WarehouseId = 1, Quantity = 350 }, new { ProductId = 6,  WarehouseId = 2, Quantity = 5 },
        new { ProductId = 7,  WarehouseId = 1, Quantity = 450 }, new { ProductId = 7,  WarehouseId = 2, Quantity = 110 },
        new { ProductId = 8,  WarehouseId = 1, Quantity = 250 }, new { ProductId = 8,  WarehouseId = 2, Quantity = 60 },
        new { ProductId = 9,  WarehouseId = 1, Quantity = 280 }, new { ProductId = 9,  WarehouseId = 2, Quantity = 3 },
        new { ProductId = 10, WarehouseId = 1, Quantity = 200 }, new { ProductId = 10, WarehouseId = 2, Quantity = 45 }
    };
}
