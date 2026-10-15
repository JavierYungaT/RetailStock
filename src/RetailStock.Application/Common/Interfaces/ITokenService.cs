using RetailStock.Domain.Entities;

namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Genera el token de acceso (JWT) para un usuario autenticado.
/// Application solo conoce el contrato; JwtTokenService (Infrastructure) firma el token.
/// Equivalente en Spring: un JwtService/JwtEncoder propio que usa el AuthController.
/// </summary>
public interface ITokenService
{
    AccessToken CreateToken(User user);
}

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
