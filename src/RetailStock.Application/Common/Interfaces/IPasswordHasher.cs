namespace RetailStock.Application.Common.Interfaces;

/// <summary>
/// Abstracción para hashear y verificar contraseñas. Application no depende de ASP.NET Core Identity:
/// la implementación (PasswordHasher&lt;User&gt;, PBKDF2) vive en Infrastructure.
/// Equivalente en Spring Security: la interfaz PasswordEncoder (encode / matches).
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string hashedPassword, string providedPassword);
}

/// <summary>
/// SuccessRehashNeeded: la contraseña es correcta pero el hash usa parámetros viejos
/// (menos iteraciones); conviene guardar un hash nuevo. Como upgradeEncoding() en Spring Security.
/// </summary>
public enum PasswordVerification
{
    Failed,
    Success,
    SuccessRehashNeeded
}
