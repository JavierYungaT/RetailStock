using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Products;
using RetailStock.Application.Warehouses;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Inventory;

/// <summary>
/// Ingresos, egresos y transferencias. Cada operación: valida, modifica Stock (la entidad aplica la regla
/// "no egresar más de lo disponible" -> InsufficientStockException 409) y registra un InventoryMovement
/// con el UserId del token.
///
/// No necesita transacción explícita: todo se guarda con UN SaveChanges, que EF ya ejecuta de forma atómica
/// (UPDATE Stocks + INSERT InventoryMovements van juntos o no va ninguno).
/// </summary>
public sealed class InventoryService : IInventoryService
{
    private readonly IStockRepository _stockRepository;
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IProductRepository _productRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IStockRepository stockRepository,
        IInventoryMovementRepository movementRepository,
        IProductRepository productRepository,
        IWarehouseRepository warehouseRepository,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _stockRepository = stockRepository;
        _movementRepository = movementRepository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<MovementResponse> RegisterEntryAsync(StockEntryRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();
        await EnsureActiveProductAsync(request.ProductId, cancellationToken);
        await EnsureWarehouseExistsAsync(request.WarehouseId, cancellationToken);

        // Si el producto nunca estuvo en esa bodega, se crea la fila de Stock en 0
        var stock = await GetOrCreateStockAsync(request.ProductId, request.WarehouseId, cancellationToken);
        stock.Increase(request.Quantity);

        var movement = InventoryMovement.CreateIn(request.ProductId, request.WarehouseId, request.Quantity, userId);
        return await SaveMovementAsync(movement, cancellationToken);
    }

    public async Task<MovementResponse> RegisterExitAsync(StockExitRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();
        await EnsureWarehouseExistsAsync(request.WarehouseId, cancellationToken);

        var stock = await GetExistingStockAsync(request.ProductId, request.WarehouseId, request.Quantity, cancellationToken);
        stock.Decrease(request.Quantity); // lanza InsufficientStockException (409) si no alcanza

        var movement = InventoryMovement.CreateOut(request.ProductId, request.WarehouseId, request.Quantity, userId);
        return await SaveMovementAsync(movement, cancellationToken);
    }

    public async Task<MovementResponse> TransferAsync(StockTransferRequest request, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredUserId();

        // Primero se crea el movimiento: valida origen != destino antes de tocar el stock
        var movement = InventoryMovement.CreateTransfer(
            request.ProductId, request.FromWarehouseId, request.ToWarehouseId, request.Quantity, userId);

        await EnsureActiveProductAsync(request.ProductId, cancellationToken);
        await EnsureWarehouseExistsAsync(request.FromWarehouseId, cancellationToken);
        await EnsureWarehouseExistsAsync(request.ToWarehouseId, cancellationToken);

        var source = await GetExistingStockAsync(request.ProductId, request.FromWarehouseId, request.Quantity, cancellationToken);
        source.Decrease(request.Quantity);

        var target = await GetOrCreateStockAsync(request.ProductId, request.ToWarehouseId, cancellationToken);
        target.Increase(request.Quantity);

        return await SaveMovementAsync(movement, cancellationToken);
    }

    public async Task<MovementResponse> GetMovementByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _movementRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(nameof(InventoryMovement), id);

    public Task<PagedResult<MovementResponse>> GetMovementsAsync(MovementQuery query, CancellationToken cancellationToken = default)
        => _movementRepository.GetPagedAsync(query, cancellationToken);

    public Task<PagedResult<StockResponse>> GetStockAsync(StockQuery query, CancellationToken cancellationToken = default)
        => _stockRepository.GetPagedAsync(query, cancellationToken);

    private async Task<MovementResponse> SaveMovementAsync(InventoryMovement movement, CancellationToken cancellationToken)
    {
        _movementRepository.Add(movement);
        // Un solo SaveChanges = una transacción: UPDATE/INSERT de Stocks + INSERT del movimiento.
        // Si otro usuario cambió el mismo Stock entre la lectura y este punto, RowVersion no coincide -> 409.
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetMovementByIdAsync(movement.Id, cancellationToken);
    }

    private async Task<Stock> GetOrCreateStockAsync(int productId, int warehouseId, CancellationToken cancellationToken)
    {
        var stock = await _stockRepository.GetAsync(productId, warehouseId, cancellationToken);
        if (stock is not null)
            return stock;

        stock = new Stock(productId, warehouseId);
        _stockRepository.Add(stock);
        return stock;
    }

    /// <summary>Sin fila de Stock = 0 unidades disponibles -> InsufficientStockException.</summary>
    private async Task<Stock> GetExistingStockAsync(int productId, int warehouseId, int requested, CancellationToken cancellationToken)
        => await _stockRepository.GetAsync(productId, warehouseId, cancellationToken)
           ?? throw new InsufficientStockException(productId, warehouseId, available: 0, requested);

    private async Task EnsureActiveProductAsync(int productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
                      ?? throw new BusinessRuleException($"El producto con id '{productId}' no existe.");
        if (!product.IsActive)
            throw new BusinessRuleException($"El producto '{product.Code}' está inactivo.");
    }

    private async Task EnsureWarehouseExistsAsync(int warehouseId, CancellationToken cancellationToken)
    {
        if (!await _warehouseRepository.ExistsAsync(warehouseId, cancellationToken))
            throw new BusinessRuleException($"La bodega con id '{warehouseId}' no existe.");
    }
}
