using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RetailStock.Infrastructure.Persistence;

/// <summary>
/// Aplica las migraciones pendientes al arrancar la API (solo se invoca en Development desde Program.cs).
/// Equivalente en Spring Boot: Flyway/Liquibase ejecutándose automáticamente al iniciar la aplicación.
///
/// En producción NO se recomienda: con varias réplicas arrancando a la vez competirían por migrar, y un
/// cambio de esquema debe ser un paso controlado del despliegue (script SQL idempotente generado con
/// "dotnet ef migrations script --idempotent" o un "migration bundle" ejecutado por el pipeline).
/// </summary>
public static class DatabaseMigrator
{
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // El DbContext es Scoped: fuera de una petición HTTP hay que crear un scope a mano
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseMigrator));

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogInformation("Base de datos al día: no hay migraciones pendientes.");
            return;
        }

        logger.LogInformation("Aplicando {Count} migraciones: {Migrations}", pending.Count, string.Join(", ", pending));
        // Crea la BD si no existe, aplica cada migración en orden y registra en __EFMigrationsHistory
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migraciones aplicadas.");
    }
}
