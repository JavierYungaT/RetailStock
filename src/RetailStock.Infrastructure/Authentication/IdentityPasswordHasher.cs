using Microsoft.AspNetCore.Identity;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;

namespace RetailStock.Infrastructure.Authentication;

/// <summary>
/// Adaptador sobre PasswordHasher&lt;User&gt; de ASP.NET Core Identity (solo esta clase, sin Identity completo).
/// Algoritmo: PBKDF2 con HMAC-SHA512, 100.000 iteraciones y salt aleatorio de 128 bits; el formato del hash
/// incluye versión e iteraciones, por eso puede detectar hashes viejos (SuccessRehashNeeded).
/// Equivalente en Spring Security: Pbkdf2PasswordEncoder.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // El hasher de Identity no usa el usuario en el algoritmo por defecto; se pasa null! para cumplir la firma
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public PasswordVerification Verify(string hashedPassword, string providedPassword)
        => _hasher.VerifyHashedPassword(null!, hashedPassword, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed
        };
}
