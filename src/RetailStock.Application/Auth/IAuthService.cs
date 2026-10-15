namespace RetailStock.Application.Auth;

/// <summary>Casos de uso de autenticación.</summary>
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}
