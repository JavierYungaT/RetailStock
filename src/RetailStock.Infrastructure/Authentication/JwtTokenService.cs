using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Authentication;

/// <summary>
/// Crea JWT firmados con HMAC-SHA256. El token lleva los claims: sub (id), email, role, jti, iat/exp.
/// Equivalente en Spring: un JwtEncoder (spring-security-oauth2-jose) o jjwt Jwts.builder().
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    // Nombres cortos de claims; los mismos se usan al validar (RoleClaimType = "role")
    public const string RoleClaim = "role";

    private readonly JwtSettings _settings;
    private readonly TimeProvider _timeProvider;

    // IOptions<T>: el Options pattern entrega la sección "Jwt" ya validada al arrancar
    public JwtTokenService(IOptions<JwtSettings> options, TimeProvider timeProvider)
    {
        _settings = options.Value;
        _timeProvider = timeProvider; // reloj inyectable: en pruebas se puede "congelar" el tiempo
    }

    public AccessToken CreateToken(User user)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.ExpirationMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,     // quién emite el token
            Audience = _settings.Audience, // para quién es
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(RoleClaim, user.Role.ToString()),
                // Id único del token: permitiría revocarlo con una lista negra
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        // JsonWebTokenHandler: el handler moderno (más rápido que JwtSecurityTokenHandler)
        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expires);
    }
}
