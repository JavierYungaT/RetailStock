using RetailStock.Domain.Entities;

namespace RetailStock.Application.Auth;

/// <summary>Puerto de persistencia de User.</summary>
public interface IUserRepository
{
    /// <summary>Con tracking: el login puede actualizar el hash (rehash).</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    void Add(User user);
}
