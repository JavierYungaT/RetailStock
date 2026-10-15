using Microsoft.Extensions.DependencyInjection;
using RetailStock.Application.Auth;
using RetailStock.Application.Categories;
using RetailStock.Application.Products;
using RetailStock.Application.Warehouses;

namespace RetailStock.Application;

/// <summary>
/// Método de extensión que registra los servicios de la capa Application en el contenedor de DI.
/// Program.cs solo llama a services.AddApplication() y no conoce los detalles internos.
/// Equivalente en Spring Boot: una clase @Configuration con @Bean / @ComponentScan del módulo.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped: una instancia por petición HTTP, igual que el DbContext del que dependen (vía repositorios).
        // Interfaz -> implementación: el controlador pide ICategoryService y recibe CategoryService.
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
