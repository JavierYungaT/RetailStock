using FluentAssertions;
using Moq;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Inventory;
using RetailStock.Application.Products;
using RetailStock.Application.Sales;
using RetailStock.Application.Warehouses;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Enums;
using RetailStock.Domain.Exceptions;
using RetailStock.Tests.TestHelpers;

namespace RetailStock.Tests.Application;

/// <summary>
/// Pruebas unitarias de SaleService: TODAS sus dependencias son mocks (Moq), así se prueba solo la
/// orquestación (validaciones, orden de pasos, qué se guarda) sin BD ni red.
/// Equivalente en Java: @ExtendWith(MockitoExtension.class) con @Mock y @InjectMocks.
///
/// xUnit crea una instancia NUEVA de la clase por cada test: el constructor hace de @BeforeEach
/// y los mocks nunca se comparten entre pruebas.
/// </summary>
public class SaleServiceTests
{
    private const int WarehouseId = 1;
    private const int CashierId = 7;

    private readonly Mock<ISaleRepository> _saleRepository = new();
    private readonly Mock<IProductRepository> _productRepository = new();
    private readonly Mock<IWarehouseRepository> _warehouseRepository = new();
    private readonly Mock<IStockRepository> _stockRepository = new();
    private readonly Mock<IInventoryMovementRepository> _movementRepository = new();
    private readonly Mock<ISaleNumberGenerator> _saleNumberGenerator = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    // Capturas: lo que el servicio "guardó" a través de los repositorios
    private Sale? _addedSale;
    private readonly List<InventoryMovement> _addedMovements = new();

    private readonly SaleService _sut; // "system under test"

    public SaleServiceTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(CashierId);
        _warehouseRepository.Setup(r => r.ExistsAsync(WarehouseId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _saleNumberGenerator.Setup(g => g.NextAsync(It.IsAny<CancellationToken>())).ReturnsAsync("FAC-000123");

        // La transacción del mock simplemente ejecuta el delegado (no hay BD que confirmar)
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<int>> operation, CancellationToken ct) => operation(ct));
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Callback = ArgumentCaptor de Mockito
        _saleRepository.Setup(r => r.Add(It.IsAny<Sale>())).Callback<Sale>(s => _addedSale = s);
        _movementRepository.Setup(r => r.Add(It.IsAny<InventoryMovement>())).Callback<InventoryMovement>(_addedMovements.Add);

        // El servicio relee la venta proyectada al final; aquí no importa su contenido
        _saleRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SaleResponse(0, "FAC-000123", DateTime.UtcNow, "", "", WarehouseId, "", CashierId, "", 0, 0, 0, false, null, Array.Empty<SaleDetailResponse>()));

        _sut = new SaleService(
            _saleRepository.Object, _productRepository.Object, _warehouseRepository.Object, _stockRepository.Object,
            _movementRepository.Object, _saleNumberGenerator.Object, _currentUser.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task CreateSaleAsync_ValidSale_CalculatesSubtotalTaxAndTotal()
    {
        // Arrange
        var rice = new Product("ABR-001", "Arroz", 1, 2.85m).WithId(1);
        var water = new Product("BEB-001", "Agua", 2, 1.10m).WithId(5);
        var riceStock = new Stock(1, WarehouseId, 500);
        var waterStock = new Stock(5, WarehouseId, 800);
        GivenProducts(rice, water);
        GivenStocks(riceStock, waterStock);

        // El arroz viene en dos líneas: el servicio debe agruparlas (2 + 1 = 3)
        var request = CreateRequest((1, 2), (5, 3), (1, 1));

        // Act
        await _sut.CreateSaleAsync(request);

        // Assert: cálculo (8.55 + 3.30 = 11.85; IVA 1.7775 -> 1.78; total 13.63)
        _addedSale.Should().NotBeNull();
        _addedSale!.Subtotal.Should().Be(11.85m);
        _addedSale.Tax.Should().Be(1.78m);
        _addedSale.Total.Should().Be(13.63m);
        _addedSale.Number.Should().Be("FAC-000123");
        _addedSale.UserId.Should().Be(CashierId, "el cajero se toma del token, no del request");
        _addedSale.Details.Should().HaveCount(2);
        _addedSale.Details.Single(d => d.ProductId == 1).UnitPrice.Should().Be(2.85m, "el precio sale del catálogo");

        // Assert: stock descontado y un movimiento Out por producto
        riceStock.Quantity.Should().Be(497);
        waterStock.Quantity.Should().Be(797);
        _addedMovements.Should().HaveCount(2)
            .And.OnlyContain(m => m.Type == MovementType.Out && m.UserId == CashierId && m.FromWarehouseId == WarehouseId);

        // Assert: todo dentro de UNA transacción y un solo SaveChanges
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSaleAsync_InsufficientStock_ThrowsInsufficientStockExceptionAndDoesNotSave()
    {
        // Arrange
        GivenProducts(new Product("BEB-002", "Coca-Cola 3 L", 2, 3.25m).WithId(6));
        GivenStocks(new Stock(6, WarehouseId, 1));
        var request = CreateRequest((6, 2));

        // Act
        var act = () => _sut.CreateSaleAsync(request);

        // Assert
        var exception = await act.Should().ThrowAsync<InsufficientStockException>();
        exception.Which.Available.Should().Be(1);
        exception.Which.Requested.Should().Be(2);
        exception.Which.Should().BeAssignableTo<ConflictException>("el middleware lo traduce a 409");

        // Nada se persistió ni se "gastó" un número de factura
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _saleNumberGenerator.Verify(g => g.NextAsync(It.IsAny<CancellationToken>()), Times.Never);
        _saleRepository.Verify(r => r.Add(It.IsAny<Sale>()), Times.Never);
    }

    [Fact]
    public async Task CreateSaleAsync_SameProductInSeveralLines_ValidatesStockAgainstTotalQuantity()
    {
        // Arrange: hay 5; cada línea pide 3 (individualmente alcanzaría, en total no)
        GivenProducts(new Product("BEB-002", "Coca-Cola 3 L", 2, 3.25m).WithId(6));
        GivenStocks(new Stock(6, WarehouseId, 5));
        var request = CreateRequest((6, 3), (6, 3));

        // Act
        var act = () => _sut.CreateSaleAsync(request);

        // Assert
        (await act.Should().ThrowAsync<InsufficientStockException>()).Which.Requested.Should().Be(6);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSaleAsync_ProductDoesNotExist_ThrowsBusinessRuleExceptionAndDoesNotSave()
    {
        // Arrange: el repositorio no encuentra el producto 99
        GivenProducts();
        GivenStocks();
        var request = CreateRequest((99, 1));

        // Act
        var act = () => _sut.CreateSaleAsync(request);

        // Assert: 400 (dato inválido en el body), no 404 (la URL /api/sales sí existe)
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*'99'*no existe*");
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSaleAsync_InactiveProduct_ThrowsBusinessRuleException()
    {
        // Arrange
        var product = new Product("ABR-001", "Arroz", 1, 2.85m).WithId(1);
        product.Deactivate();
        GivenProducts(product);
        GivenStocks(new Stock(1, WarehouseId, 500));

        // Act
        var act = () => _sut.CreateSaleAsync(CreateRequest((1, 1)));

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*inactivo*");
    }

    [Fact]
    public async Task CreateSaleAsync_WarehouseDoesNotExist_ThrowsBusinessRuleExceptionBeforeLoadingProducts()
    {
        // Arrange
        var request = CreateRequest((1, 1)) with { WarehouseId = 42 };

        // Act
        var act = () => _sut.CreateSaleAsync(request);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*bodega*42*");
        _productRepository.Verify(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- helpers de Arrange ----------

    private void GivenProducts(params Product[] products)
        => _productRepository
            .Setup(r => r.GetByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

    private void GivenStocks(params Stock[] stocks)
        => _stockRepository
            .Setup(r => r.GetByWarehouseAsync(WarehouseId, It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

    private static CreateSaleRequest CreateRequest(params (int ProductId, int Quantity)[] items) => new()
    {
        CustomerName = "Juan Pérez",
        CustomerId = "0912345678",
        WarehouseId = WarehouseId,
        Items = items.Select(i => new SaleItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList()
    };
}
