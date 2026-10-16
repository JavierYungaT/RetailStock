using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Sap;
using RetailStock.Domain.Entities;
using RetailStock.Infrastructure.Common;
using RetailStock.Infrastructure.Persistence;
using RetailStock.Infrastructure.Persistence.Repositories;

namespace RetailStock.Tests.Application;

/// <summary>
/// Pruebas de SapSyncService con:
///  - SAP MOCKEADO (Moq): controlamos qué factura falla, sin red.
///  - BD EF Core InMemory + el SaleRepository REAL: el sync depende de que el repositorio cargue ventas con
///    Details y Product (Include/ThenInclude) y de que SaveChanges persista el marcado. Con InMemory eso se
///    prueba de verdad, en vez de armar a mano grafos de entidades con setters privados.
///
/// IDisposable = @AfterEach: cada test usa su propia BD en memoria (nombre único) y la descarta.
/// </summary>
public class SapSyncServiceTests : IDisposable
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly AppDbContext _context;
    private readonly Mock<ISapServiceLayerClient> _sapClient = new();
    private readonly Mock<IBusinessClock> _businessClock = new();
    private readonly FakeLogger<SapSyncService> _logger = new(); // logger de pruebas: guarda lo registrado
    private readonly SapSyncService _sut;

    public SapSyncServiceTests()
    {
        _context = CreateContext();
        _context.Database.EnsureCreated(); // crea el "esquema" y aplica el seed (productos ABR-001, BEB-001...)

        _businessClock.Setup(c => c.ToBusinessDate(It.IsAny<DateTime>())).Returns(new DateOnly(2026, 10, 15));
        _sapClient.Setup(c => c.LoginAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new SapSession("session", "1000190", 30));
        _sapClient.Setup(c => c.FindInvoiceByReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((SapInvoiceResult?)null);

        _sut = new SapSyncService(new SaleRepository(_context, Options.Create(new BusinessSettings { UtcOffsetMinutes = -300 })), _sapClient.Object, _businessClock.Object, _context, _logger);
    }

    [Fact]
    public async Task SyncPendingSalesAsync_OneInvoiceFails_LogsErrorAndSyncsTheOthers()
    {
        // Arrange: 3 ventas pendientes; SAP rechaza la segunda
        await SeedSalesAsync("FAC-000001", "FAC-000002", "FAC-000003");
        var docEntry = 100;
        _sapClient
            .Setup(c => c.CreateInvoiceAsync(It.IsAny<SapInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new SapInvoiceResult(++docEntry, 50000 + docEntry));
        _sapClient
            .Setup(c => c.CreateInvoiceAsync(It.Is<SapInvoiceRequest>(i => i.NumAtCard == "FAC-000002"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SapIntegrationException("Invalid item 'X'", 400, "-10"));

        // Act
        var result = await _sut.SyncPendingSalesAsync(batchSize: 50);

        // Assert: resultado de la corrida
        result.Processed.Should().Be(3);
        result.Succeeded.Should().Be(2);
        result.Failed.Should().Be(1);
        result.Items.Single(i => !i.Success).SaleNumber.Should().Be("FAC-000002");

        // Assert: lo que quedó PERSISTIDO (se lee con un contexto nuevo, no con el que tiene las entidades en memoria)
        await using var verification = CreateContext();
        var sales = await verification.Sales.OrderBy(s => s.Number).ToListAsync();
        sales.Select(s => (s.Number, s.SyncedToSap, s.SapDocEntry)).Should().Equal(
            ("FAC-000001", true, 101),
            ("FAC-000002", false, (int?)null),
            ("FAC-000003", true, 102));

        // Assert: el fallo se registró como Error, con el número de la venta
        _logger.Collector.GetSnapshot().Should().ContainSingle(r => r.Level == LogLevel.Error)
            .Which.Message.Should().Contain("FAC-000002");
    }

    [Fact]
    public async Task SyncPendingSalesAsync_ValidSale_SendsInvoiceInServiceLayerFormat()
    {
        // Arrange
        await SeedSalesAsync("FAC-000001");
        SapInvoiceRequest? sent = null;
        _sapClient
            .Setup(c => c.CreateInvoiceAsync(It.IsAny<SapInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<SapInvoiceRequest, CancellationToken>((invoice, _) => sent = invoice)
            .ReturnsAsync(new SapInvoiceResult(1, 50001));

        // Act
        await _sut.SyncPendingSalesAsync(50);

        // Assert
        sent.Should().NotBeNull();
        sent!.CardCode.Should().Be("C0912345678");
        sent.NumAtCard.Should().Be("FAC-000001");
        sent.DocDate.Should().Be("2026-10-15", "la fecha contable es el día local del negocio");
        sent.DocumentLines.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            ItemCode = "ABR-001",  // Product.Code cargado con Include
            Quantity = 2m,
            UnitPrice = 2.85m,     // sin IVA
            WarehouseCode = "01",
            TaxCode = "IVA15"
        });
    }

    [Fact]
    public async Task SyncPendingSalesAsync_InvoiceAlreadyExistsInSap_MarksSaleWithoutCreatingDuplicate()
    {
        // Arrange: la corrida anterior creó la factura en SAP pero no alcanzó a marcar la venta
        await SeedSalesAsync("FAC-000001");
        _sapClient
            .Setup(c => c.FindInvoiceByReferenceAsync("FAC-000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SapInvoiceResult(77, 50077));

        // Act
        var result = await _sut.SyncPendingSalesAsync(50);

        // Assert
        result.Items.Should().ContainSingle().Which.AlreadyExistedInSap.Should().BeTrue();
        _sapClient.Verify(c => c.CreateInvoiceAsync(It.IsAny<SapInvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        (await CreateContext().Sales.SingleAsync()).SapDocEntry.Should().Be(77);
    }

    [Fact]
    public async Task SyncPendingSalesAsync_NoPendingSales_DoesNotCallSap()
    {
        // Act
        var result = await _sut.SyncPendingSalesAsync(50);

        // Assert
        result.Processed.Should().Be(0);
        _sapClient.VerifyNoOtherCalls(); // ni siquiera Login
    }

    [Fact]
    public async Task SyncPendingSalesAsync_SapLoginFails_ThrowsAndDoesNotTouchSales()
    {
        // Arrange
        await SeedSalesAsync("FAC-000001");
        _sapClient
            .Setup(c => c.LoginAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SapIntegrationException("Fail to get DB Credentials", 401));

        // Act
        var act = () => _sut.SyncPendingSalesAsync(50);

        // Assert: falla rápido (el endpoint responde 502) y la venta sigue pendiente
        await act.Should().ThrowAsync<SapIntegrationException>();
        _sapClient.Verify(c => c.CreateInvoiceAsync(It.IsAny<SapInvoiceRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        (await CreateContext().Sales.SingleAsync()).SyncedToSap.Should().BeFalse();
    }

    [Fact]
    public async Task SyncPendingSalesAsync_BatchSizeSmallerThanPending_ProcessesOldestFirst()
    {
        // Arrange
        await SeedSalesAsync("FAC-000001", "FAC-000002", "FAC-000003");
        _sapClient
            .Setup(c => c.CreateInvoiceAsync(It.IsAny<SapInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SapInvoiceResult(1, 1));

        // Act
        var result = await _sut.SyncPendingSalesAsync(batchSize: 2);

        // Assert
        result.Items.Select(i => i.SaleNumber).Should().Equal("FAC-000001", "FAC-000002");
    }

    // ---------- helpers ----------

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            // EnableNullChecks(false): RowVersion lo genera SQL Server; el proveedor InMemory no sabe generarlo
            .UseInMemoryDatabase(_databaseName, inMemory => inMemory.EnableNullChecks(false))
            .Options;
        return new AppDbContext(options);
    }

    private async Task SeedSalesAsync(params string[] numbers)
    {
        foreach (var number in numbers)
        {
            var sale = new Sale(number, "Juan Pérez", "0912345678", warehouseId: 1, userId: 3);
            sale.AddDetail(productId: 1, quantity: 2, unitPrice: 2.85m); // producto 1 = ABR-001 del seed
            _context.Sales.Add(sale);
        }
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
