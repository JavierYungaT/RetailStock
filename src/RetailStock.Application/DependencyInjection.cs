using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

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
        // Registra automáticamente todos los validadores (AbstractValidator<T>) de este ensamblado.
        // Similar a como Spring detecta beans por escaneo de paquetes.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Los servicios de casos de uso (ProductService, SaleService...) se registran aquí desde la Fase 3.

        return services;
    }
}
