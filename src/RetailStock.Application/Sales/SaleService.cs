using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Inventory;
using RetailStock.Application.Products;
using RetailStock.Application.Warehouses;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Sales;

/// <summary>
/// Registro de ventas. Todo ocurre dentro de UNA transacción (ExecuteInTransactionAsync):
///   1. Valida bodega, productos (existen y activos) y stock de cada ítem.
///   2. Descuenta stock (Stock.Decrease -> InsufficientStockException 409).
///   3. Obtiene el número de factura (SEQUENCE).
///   4. Crea la venta: Sale.AddDetail calcula subtotal, IVA 15% y total con decimal y redondeo a 2 decimales.
///   5. Registra un movimiento Out por producto.
///   6. SaveChanges + Commit. Si cualquier paso falla -> Rollback: no queda ni venta ni stock descontado.
/// Equivalente en Spring: un método @Service @Transactional(rollbackFor = Exception.class).
/// </summary>
public sealed class SaleService : ISaleService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly ISaleNumberGenerator _saleNumberGenerator;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public SaleService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository,
        IStockRepository stockRepository,
        IInventoryMovementRepository movementRepository,
        ISaleNumberGenerator saleNumberGenerator,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _saleNumberGenerator = saleNumberGenerator;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<SaleResponse> CreateSaleAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();

        // Si el mismo producto viene en dos líneas, se suman: así se valida el stock contra el total pedido
        var items = request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => (ProductId: g.Key, Quantity: g.Sum(i => i.Quantity)))
            .ToList();

        var saleId = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (!await _warehouseRepository.ExistsAsync(request.WarehouseId, ct))
                throw new BusinessRuleException($"La bodega con id '{request.WarehouseId}' no existe.");

            var productIds = items.Select(i => i.ProductId).ToList();

            // 2 consultas en total (productos y stocks), sin importar cuántos ítems tenga la venta: sin N+1
            var products = (await _productRepository.GetByIdsAsync(productIds, ct)).ToDictionary(p => p.Id);
            var stocks = (await _stockRepository.GetByWarehouseAsync(request.WarehouseId, productIds, ct)).ToDictionary(s => s.ProductId);

            var lines = new List<(Product Product, int Quantity)>();
            foreach (var (productId, quantity) in items)
            {
                if (!products.TryGetValue(productId, out var product))
                    throw new BusinessRuleException($"El producto con id '{productId}' no existe.");
                if (!product.IsActive)
                    throw new BusinessRuleException($"El producto '{product.Code}' está inactivo y no se puede vender.");

                if (!stocks.TryGetValue(productId, out var stock))
                    throw new InsufficientStockException(productId, request.WarehouseId, available: 0, quantity);

                stock.Decrease(quantity); // regla de dominio: lanza InsufficientStockException si no alcanza
                lines.Add((product, quantity));
            }

            // El número se pide DESPUÉS de validar todo, para no "quemar" números en ventas rechazadas
            var number = await _saleNumberGenerator.NextAsync(ct);
            var sale = new Sale(number, request.CustomerName, request.CustomerId, request.WarehouseId, userId);

            foreach (var (product, quantity) in lines)
            {
                // Precio del catálogo (no del request); Sale recalcula Subtotal / Tax / Total en cada línea
                sale.AddDetail(product.Id, quantity, product.Price);
                _movementRepository.Add(InventoryMovement.CreateOut(product.Id, request.WarehouseId, quantity, userId));
            }

            _saleRepository.Add(sale);

            // INSERT Sale + SaleDetails + InventoryMovements y UPDATE Stocks (con chequeo de RowVersion)
            await _unitOfWork.SaveChangesAsync(ct);
            return sale.Id;
        }, cancellationToken);

        return await GetByIdAsync(saleId, cancellationToken);
    }

    public async Task<SaleResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _saleRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(nameof(Sale), id);

    public Task<PagedResult<SaleSummaryResponse>> GetPagedAsync(SaleQuery query, CancellationToken cancellationToken = default)
        => _saleRepository.GetPagedAsync(query, cancellationToken);
}
