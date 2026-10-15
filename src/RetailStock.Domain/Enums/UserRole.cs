namespace RetailStock.Domain.Enums;

/// <summary>
/// Roles del sistema. Se guardan como texto en la BD ("Admin", no 0) para que sea legible.
/// Equivalente en JPA: un enum con @Enumerated(EnumType.STRING).
/// </summary>
public enum UserRole
{
    Admin,
    Warehouse,
    Cashier
}
