using RetailStock.Application.Auth;
using RetailStock.Application.Common.Dtos;

namespace RetailStock.Application.Users;

/// <summary>Gestión de usuarios (solo Admin). No hay borrado: se desactivan para conservar su historial.</summary>
public interface IUserService
{
    Task<PagedResult<UserResponse>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default);
    Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
