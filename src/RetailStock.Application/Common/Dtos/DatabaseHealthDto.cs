namespace RetailStock.Application.Common.Dtos;

/// <summary>
/// DTO inmutable con el resultado del chequeo de base de datos.
/// "record" = clase inmutable con igualdad por valor; equivalente a un record de Java 16+
/// (o a un DTO con Lombok @Value).
/// </summary>
public sealed record DatabaseHealthDto(
    bool CanConnect,
    bool DatabaseExists,
    string? ServerVersion,
    string? Error);
