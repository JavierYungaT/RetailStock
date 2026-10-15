using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Infrastructure.Persistence;

namespace RetailStock.Infrastructure;

/// <summary>
/// Registra los servicios de infraestructura: acceso a datos, JWT, clientes HTTP externos.
/// Equivalente en Spring Boot: una clase @Configuration que define DataSource, repositorios, RestClient, etc.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Lee "ConnectionStrings:DefaultConnection" de appsettings (como spring.datasource.url)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'DefaultConnection'.");

        // Ciclos de vida en .NET (equivalencias con Spring):
        //   AddSingleton -> @Scope("singleton") (una instancia para toda la app)
        //   AddScoped    -> @RequestScope (una instancia por petición HTTP)
        //   AddTransient -> @Scope("prototype") (nueva instancia cada vez)
        services.AddSingleton<IDatabaseHealthChecker>(_ => new DatabaseHealthChecker(connectionString));

        // Registra AppDbContext como Scoped (una instancia por petición), igual que el EntityManager en Spring.
        // Equivale a configurar spring.datasource.* + spring.jpa.* con el dialecto de SQL Server.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                // Las migraciones viven en este proyecto (Infrastructure), no en Api
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                // Reintenta ante errores transitorios de red/BD (útil con Docker y en la nube)
                sql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // Fase 4: generador JWT · Fase 6: repositorios Dapper · Fase 7: cliente SAP (HttpClient tipado)

        return services;
    }
}
