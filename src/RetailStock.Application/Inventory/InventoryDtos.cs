using System.ComponentModel.DataAnnotations;
using RetailStock.Application.Common.Dtos;
using RetailStock.Domain.Enums;

namespace RetailStock.Application.Inventory;

/// <summary>Ingreso de mercadería a una bodega (compra a proveedor, devolución...).</summary>
public sealed record StockEntryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un producto válido.")]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una bodega válida.")]
    public int WarehouseId { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe estar entre 1 y 1.000.000.")]
    public int Quantity { get; init; }
}

/// <summary>Egreso de mercadería (merma, consumo interno, ajuste...).</summary>
public sealed record StockExitRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un producto válido.")]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar una bodega válida.")]
    public int WarehouseId { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe estar entre 1 y 1.000.000.")]
    public int Quantity { get; init; }
}

public sealed record StockTransferRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar un producto válido.")]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar la bodega de origen.")]
    public int FromWarehouseId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Debe indicar la bodega de destino.")]
    public int ToWarehouseId { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "La cantidad debe estar entre 1 y 1.000.000.")]
    public int Quantity { get; init; }
}

public sealed record MovementResponse(
    int Id,
    MovementType Type,
    int ProductId,
    string ProductCode,
    string ProductName,
    int? FromWarehouseId,
    string? FromWarehouseName,
    int? ToWarehouseId,
    string? ToWarehouseName,
    int Quantity,
    DateTime Date,
    int UserId,
    string UserEmail);

public sealed record StockResponse(
    int ProductId,
    string ProductCode,
    string ProductName,
    int CategoryId,
    string CategoryName,
    decimal Price,
    bool IsActive,
    int WarehouseId,
    string WarehouseName,
    int Quantity);

/// <summary>Filtros de existencias: "name" busca por nombre o código de producto.</summary>
public sealed record StockQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? WarehouseId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    /// <summary>true = oculta productos desactivados (el punto de venta solo muestra lo vendible).</summary>
    public bool OnlyActive { get; init; }
}

/// <summary>Filtros del kardex de movimientos.</summary>
public sealed record MovementQuery : PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    /// <summary>Movimientos donde la bodega es origen O destino.</summary>
    [Range(1, int.MaxValue)]
    public int? WarehouseId { get; init; }

    public MovementType? Type { get; init; }
}
