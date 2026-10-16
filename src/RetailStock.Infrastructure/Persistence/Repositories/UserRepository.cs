using Microsoft.EntityFrameworkCore;
using RetailStock.Application.Auth;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Users;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Enums;

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

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<PagedResult<User>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var users = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
            users = users.Where(u => u.Email.Contains(query.Name.Trim()));
        if (query.Role is not null)
            users = users.Where(u => u.Role == query.Role);
        if (query.IsActive is not null)
            users = users.Where(u => u.IsActive == query.IsActive);

        return users
            .OrderBy(u => u.Email)
            .ThenBy(u => u.Id)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
        => _context.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive, cancellationToken);

    public void Add(User user) => _context.Users.Add(user);
}
