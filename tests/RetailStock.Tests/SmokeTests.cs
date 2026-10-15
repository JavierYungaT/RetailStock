using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RetailStock.Application;

namespace RetailStock.Tests;

/// <summary>
/// Prueba de humo: verifica que xUnit, FluentAssertions y la referencia a Application funcionan.
/// [Fact] = @Test de JUnit 5. Las pruebas reales del SaleService llegan en la Fase 8.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void AddApplication_ShouldRegisterServicesWithoutErrors()
    {
        var services = new ServiceCollection();

        var act = () => services.AddApplication().BuildServiceProvider();

        act.Should().NotThrow();
    }
}
