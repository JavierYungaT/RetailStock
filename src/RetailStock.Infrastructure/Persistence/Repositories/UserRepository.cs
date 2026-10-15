using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Auth;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Persistence.Repositories;

/// <summary>Implementación EF Core de IUserRepository (como un UserDetailsService que lee de JPA).</summary>
internal sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(User user) => _context.Users.Add(user);
}
