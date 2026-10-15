using FluentAssertions;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Tests.Domain;

public class StockTests
{
    [Fact]
    public void Decrease_QuantityGreaterThanAvailable_ThrowsInsufficientStockExceptionAndKeepsQuantity()
    {
        // Arrange
        var stock = new Stock(productId: 1, warehouseId: 2, quantity: 5);

        // Act
        var act = () => stock.Decrease(6);

        // Assert
        act.Should().Throw<InsufficientStockException>()
            .Which.Should().BeEquivalentTo(new { ProductId = 1, WarehouseId = 2, Available = 5, Requested = 6 });
        stock.Quantity.Should().Be(5);
    }

    [Fact]
    public void Decrease_ExactlyAvailableQuantity_LeavesZero()
    {
        // Arrange
        var stock = new Stock(1, 1, 5);

        // Act
        stock.Decrease(5);

        // Assert
        stock.Quantity.Should().Be(0);
    }

    [Fact]
    public void InsufficientStockException_IsAConflict_SoTheApiReturns409()
    {
        // La jerarquía de excepciones es parte del contrato HTTP: este test protege el 409
        typeof(InsufficientStockException).Should().BeDerivedFrom<ConflictException>();
    }
}
