using RetailStock.Application.Common.Exceptions;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Auth;

/// <summary>
/// Login y registro. Equivalente en Spring Security: AuthenticationManager + UserDetailsService +
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

        if (verification == PasswordVerification.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(_passwordHasher.Hash(request.Password));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var token = _tokenService.CreateToken(user);
        return new LoginResponse(token.Token, "Bearer", token.ExpiresAtUtc, user.Email, user.Role.ToString());
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
            throw new ConflictException($"Ya existe un usuario con el email '{email}'.");

        // Role no es null aquí: [Required] ya lo validó en el controlador
        var user = new User(email, _passwordHasher.Hash(request.Password), request.Role!.Value);
        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponse(user.Id, user.Email, user.Role.ToString());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
