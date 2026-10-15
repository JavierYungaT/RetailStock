using RetailStock.Domain.Enums;

namespace RetailStock.Domain.Entities;

/// <summary>
/// Usuario del sistema. Solo guarda el HASH de la contraseña, nunca el texto plano.
/// Las entidades son clases POCO sin atributos de EF: el mapeo vive en Infrastructure (Fluent API).
/// Equivalente en Spring: un @Entity, pero sin anotaciones JPA en el dominio.
/// </summary>
public class User
{
    public int Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }

    // Constructor vacío privado: lo usa EF Core para materializar filas (como el constructor sin args que exige JPA)
    private User() { }

    public User(string email, string passwordHash, UserRole role)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
    }
}
