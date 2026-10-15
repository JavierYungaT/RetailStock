using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Warehouses;

/// <summary>
/// Casos de uso de Warehouse. DELETE es físico, pero solo si la bodega no tiene stock,
/// movimientos ni ventas; si los tiene -> 409 Conflict.
/// </summary>
public sealed class WarehouseService : IWarehouseService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WarehouseService(IWarehouseRepository warehouseRepository, IUnitOfWork unitOfWork)
    {
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<WarehouseResponse>> GetPagedAsync(PaginationQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _warehouseRepository.GetPagedAsync(query, cancellationToken);
        return page.Map(w => w.ToResponse());
    }

    public async Task<WarehouseResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => (await GetExistingAsync(id, cancellationToken)).ToResponse();

    public async Task<WarehouseResponse> CreateAsync(CreateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureNameIsUniqueAsync(request.Name, null, cancellationToken);

        var warehouse = new Warehouse(request.Name, request.Location);
        _warehouseRepository.Add(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return warehouse.ToResponse();
    }

    public async Task<WarehouseResponse> UpdateAsync(int id, UpdateWarehouseRequest request, CancellationToken cancellationToken = default)
    {
        var warehouse = await GetExistingAsync(id, cancellationToken);
        await EnsureNameIsUniqueAsync(request.Name, id, cancellationToken);

        warehouse.Update(request.Name, request.Location);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return warehouse.ToResponse();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var warehouse = await GetExistingAsync(id, cancellationToken);

        if (await _warehouseRepository.IsInUseAsync(id, cancellationToken))
            throw new ConflictException($"La bodega '{warehouse.Name}' tiene stock, movimientos o ventas y no se puede eliminar.");

        _warehouseRepository.Remove(warehouse);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Warehouse> GetExistingAsync(int id, CancellationToken cancellationToken)
        => await _warehouseRepository.GetByIdAsync(id, cancellationToken)
           ?? throw new NotFoundException(nameof(Warehouse), id);

    private async Task EnsureNameIsUniqueAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        if (await _warehouseRepository.NameExistsAsync(name.Trim(), excludeId, cancellationToken))
            throw new ConflictException($"Ya existe una bodega con el nombre '{name.Trim()}'.");
    }
}
