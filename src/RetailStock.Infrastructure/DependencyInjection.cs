using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RetailStock.Application.Auth;
using RetailStock.Application.Categories;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Products;
using RetailStock.Application.Warehouses;
using RetailStock.Infrastructure.Authentication;
using RetailStock.Infrastructure.Persistence;
using RetailStock.Infrastructure.Persistence.Repositories;

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

        // IUnitOfWork resuelve al MISMO AppDbContext de la petición que usan los repositorios;
        // así SaveChanges confirma todo lo que los repositorios agregaron/modificaron.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Repositorios: Scoped porque dependen del DbContext (Scoped)
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // ---- Autenticación ----
        // Options pattern: enlaza la sección "Jwt" a JwtSettings, valida sus DataAnnotations y falla AL ARRANCAR
        // si falta la clave (mejor que descubrirlo en el primer login). Como @ConfigurationProperties + @Validated.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        // Singleton: no guardan estado por petición y son thread-safe
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        // Fase 6: repositorios Dapper · Fase 7: cliente SAP (HttpClient tipado)

        return services;
    }
}
