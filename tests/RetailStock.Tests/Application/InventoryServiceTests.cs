using FluentAssertions;
using Moq;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Inventory;
using RetailStock.Application.Products;
using RetailStock.Application.Warehouses;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Enums;
using RetailStock.Domain.Exceptions;
using RetailStock.Tests.TestHelpers;

namespace RetailStock.Tests.Application;

public class InventoryServiceTests
{
    private const int UserId = 2;

    private readonly Mock<IStockRepository> _stockRepository = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<InventoryMovement> _addedMovements = new();
    private readonly InventoryService _sut;

    public InventoryServiceTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(UserId);
        _warehouseRepository.Setup(r => r.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _productRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => new Product($"P-{id}", "Producto", 1, 1m).WithId(id));
        _movementRepository.Setup(r => r.Add(It.IsAny<InventoryMovement>())).Callback<InventoryMovement>(_addedMovements.Add);
        _movementRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovementResponse(1, MovementType.Out, 1, "P-1", "Producto", 1, "B1", null, null, 1, DateTime.UtcNow, UserId, "x"));

        _sut = new InventoryService(_stockRepository.Object, _movementRepository.Object, _productRepository.Object,
            _warehouseRepository.Object, _currentUser.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task RegisterExitAsync_QuantityGreaterThanStock_ThrowsInsufficientStockExceptionAndDoesNotSave()
    {
        // Arrange
        var stock = new Stock(productId: 9, warehouseId: 2, quantity: 3);
        GivenStock(stock);
        var request = new StockExitRequest { ProductId = 9, WarehouseId = 2, Quantity = 100 };

        // Act
        var act = () => _sut.RegisterExitAsync(request);

        // Assert
        var exception = await act.Should().ThrowAsync<InsufficientStockException>();
        exception.Which.Available.Should().Be(3);
        stock.Quantity.Should().Be(3, "el stock no debe modificarse si la operación falla");
        _addedMovements.Should().BeEmpty();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterExitAsync_ProductNeverStockedInWarehouse_ThrowsInsufficientStockWithZeroAvailable()
    {
        // Arrange: no hay fila de Stock para ese producto/bodega
        _stockRepository.Setup(r => r.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Stock?)null);

        // Act
        var act = () => _sut.RegisterExitAsync(new StockExitRequest { ProductId = 1, WarehouseId = 2, Quantity = 1 });

        // Assert
        (await act.Should().ThrowAsync<InsufficientStockException>()).Which.Available.Should().Be(0);
    }

    [Fact]
    public async Task RegisterExitAsync_ValidQuantity_DecreasesStockAndRegistersOutMovementWithTokenUser()
    {
        // Arrange
        var stock = new Stock(1, 2, 10);
        GivenStock(stock);

        // Act
        await _sut.RegisterExitAsync(new StockExitRequest { ProductId = 1, WarehouseId = 2, Quantity = 4 });

        // Assert
        stock.Quantity.Should().Be(6);
        _addedMovements.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new { Type = MovementType.Out, ProductId = 1, FromWarehouseId = 2, Quantity = 4, UserId });
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TransferAsync_SameSourceAndTarget_ThrowsBusinessRuleExceptionWithoutTouchingStock()
    {
        // Act
        var act = () => _sut.TransferAsync(new StockTransferRequest { ProductId = 1, FromWarehouseId = 1, ToWarehouseId = 1, Quantity = 1 });

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
        _stockRepository.Verify(r => r.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransferAsync_TargetHasNoStockRow_CreatesItAndMovesQuantity()
    {
        // Arrange
        var source = new Stock(1, 1, 10);
        _stockRepository.Setup(r => r.GetAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _stockRepository.Setup(r => r.GetAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync((Stock?)null);
        Stock? createdTarget = null;
        _stockRepository.Setup(r => r.Add(It.IsAny<Stock>())).Callback<Stock>(s => createdTarget = s);

        // Act
        await _sut.TransferAsync(new StockTransferRequest { ProductId = 1, FromWarehouseId = 1, ToWarehouseId = 2, Quantity = 4 });

        // Assert
        source.Quantity.Should().Be(6);
        createdTarget.Should().NotBeNull();
        createdTarget!.WarehouseId.Should().Be(2);
        createdTarget.Quantity.Should().Be(4);
        _addedMovements.Should().ContainSingle().Which.Type.Should().Be(MovementType.Transfer);
    }

    private void GivenStock(Stock stock)
        => _stockRepository
            .Setup(r => r.GetAsync(stock.ProductId, stock.WarehouseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);
}
