using RetailStock.Domain.Entities;

namespace RetailStock.Application.Warehouses;

/// <summary>Mapeo manual Warehouse -> WarehouseResponse.</summary>
public static class WarehouseMappings
{
    public static WarehouseResponse ToResponse(this Warehouse warehouse)
        => new(warehouse.Id, warehouse.Name, warehouse.Location);
}
