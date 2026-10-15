using FluentAssertions;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Tests.Domain;

/// <summary>
/// Pruebas del cálculo de la venta en la ENTIDAD (sin mocks ni BD): la regla del IVA vive en el dominio,
/// así que se prueba donde vive. Son las pruebas más rápidas y estables de la pirámide.
/// </summary>
public class SaleTests
{
    // [Theory] + [MemberData] = @ParameterizedTest + @MethodSource de JUnit 5.
    // decimal no puede ir en [InlineData] (no es una constante de atributo), por eso TheoryData.
    public static TheoryData<int, decimal, decimal, decimal, decimal> TaxCases => new()
    {
        // cantidad, precio, subtotal esperado, IVA esperado, total esperado
        { 1, 2.85m, 2.85m, 0.43m, 3.28m },     // 0.4275 -> 0.43
        { 10, 1.10m, 11.00m, 1.65m, 12.65m },  // exacto
        { 3, 0.10m, 0.30m, 0.05m, 0.35m },     // 0.045 -> 0.05 (redondeo comercial; el "bancario" daría 0.04)
        { 7, 0.33m, 2.31m, 0.35m, 2.66m }      // 0.3465 -> 0.35
    };

    [Theory]
    [MemberData(nameof(TaxCases))]
    public void AddDetail_SingleLine_CalculatesSubtotalTaxAndTotalRoundedToTwoDecimals(
        int quantity, decimal unitPrice, decimal expectedSubtotal, decimal expectedTax, decimal expectedTotal)
    {
        // Arrange
        var sale = new Sale("FAC-000001", "Cliente", "0912345678", warehouseId: 1, userId: 1);

        // Act
        sale.AddDetail(productId: 1, quantity, unitPrice);

        // Assert
        sale.Subtotal.Should().Be(expectedSubtotal);
        sale.Tax.Should().Be(expectedTax);
        sale.Total.Should().Be(expectedTotal);
    }

    [Fact]
    public void AddDetail_SeveralLines_AccumulatesTotals()
    {
        // Arrange
        var sale = new Sale("FAC-000001", "Cliente", "0912345678", 1, 1);

        // Act
        sale.AddDetail(1, 3, 2.85m); // 8.55
        sale.AddDetail(2, 3, 1.10m); // 3.30

        // Assert
        sale.Details.Should().HaveCount(2);
        sale.Subtotal.Should().Be(11.85m);
        sale.Tax.Should().Be(1.78m);    // 1.7775 -> 1.78
        sale.Total.Should().Be(13.63m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddDetail_NonPositiveQuantity_ThrowsBusinessRuleException(int quantity)
    {
        // Arrange
        var sale = new Sale("FAC-000001", "Cliente", "0912345678", 1, 1);

        // Act
        var act = () => sale.AddDetail(1, quantity, 1.00m);

        // Assert
        act.Should().Throw<BusinessRuleException>();
        sale.Details.Should().BeEmpty();
    }

    [Fact]
    public void MarkAsSyncedToSap_AlreadySynced_ThrowsBusinessRuleException()
    {
        // Arrange
        var sale = new Sale("FAC-000001", "Cliente", "0912345678", 1, 1);
        sale.MarkAsSyncedToSap(10);

        // Act
        var act = () => sale.MarkAsSyncedToSap(11);

        // Assert
        act.Should().Throw<BusinessRuleException>();
        sale.SapDocEntry.Should().Be(10);
    }
}
