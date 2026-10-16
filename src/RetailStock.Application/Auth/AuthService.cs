using RetailStock.Application.Common.Exceptions;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;

namespace RetailStock.Application.Auth;

/// <summary>
/// Login (la gestión de usuarios vive en UserService). Equivalente en Spring Security: AuthenticationManager + UserDetailsService +
/// PasswordEncoder, pero orquestados a mano (sin Identity completo).
/// </summary>
public sealed class AuthService : IAuthService
{
    // Un hash PBKDF2 válido cualquiera (su valor no importa): se verifica contra él cuando el email no existe,
    // para que la respuesta tarde lo mismo y no revele qué emails están registrados (ataque de tiempo).
    private const string DummyHash = "AQAAAAIAAYagAAAAELbMoKWVpVn1S5usBUaqzR3GapPU3y+RMsJyFCuJijhvE7YA9ueA8Jex/9HPe3Iimg==";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        if (user is null)
        {
            _passwordHasher.Verify(DummyHash, request.Password); // mismo costo que un login real
            throw new InvalidCredentialsException();
        }

        var verification = _passwordHasher.Verify(user.PasswordHash, request.Password);
        if (verification == PasswordVerification.Failed)
            throw new InvalidCredentialsException();

        // Se informa DESPUÉS de verificar la contraseña: así no revela a un extraño qué cuentas existen
        if (!user.IsActive)
            throw new InvalidCredentialsException("Tu cuenta está desactivada. Contacta al administrador.");

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(_passwordHasher.Hash(request.Password));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var token = _tokenService.CreateToken(user);
        return new LoginResponse(token.Token, "Bearer", token.ExpiresAtUtc, user.Email, user.Role.ToString());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
