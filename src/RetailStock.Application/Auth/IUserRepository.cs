using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Users;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Auth;

/// <summary>Puerto de persistencia de User.</summary>
public interface IUserRepository
{
    /// <summary>Con tracking: el login puede actualizar el hash (rehash).</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Con tracking: para editar rol, estado o contraseña.</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default);

    /// <summary>Administradores activos (para impedir que el sistema se quede sin ninguno).</summary>
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);

    void Add(User user);
}
