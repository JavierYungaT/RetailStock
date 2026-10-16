using RetailStock.Application.Auth;
using RetailStock.Application.Common.Dtos;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Enums;
using RetailStock.Domain.Exceptions;

namespace RetailStock.Application.Users;

/// <summary>
/// Casos de uso de usuarios. Reglas para que el sistema nunca quede sin administración:
///  - Un admin no puede desactivarse ni quitarse el rol Admin a sí mismo.
///  - No se puede desactivar ni degradar al ÚLTIMO administrador activo.
/// </summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
        => (await _userRepository.GetPagedAsync(query, cancellationToken)).Map(ToResponse);

    public async Task<UserResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => ToResponse(await GetExistingAsync(id, cancellationToken));

    public async Task<UserResponse> CreateAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.EmailExistsAsync(email, cancellationToken))
            throw new ConflictException($"Ya existe un usuario con el email '{email}'.");

        // Role no es null: [Required] ya lo validó en el controlador
        var user = new User(email, _passwordHasher.Hash(request.Password), request.Role!.Value);
        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetExistingAsync(id, cancellationToken);
        var newRole = request.Role!.Value;
        var losesAdmin = user.Role == UserRole.Admin && user.IsActive && (newRole != UserRole.Admin || !request.IsActive);

        if (id == _currentUser.UserId && losesAdmin)
            throw new BusinessRuleException("No puedes desactivarte ni quitarte el rol de administrador a ti mismo.");

        if (losesAdmin && await _userRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
            throw new ConflictException("Debe existir al menos un administrador activo.");

        user.ChangeRole(newRole);
        if (request.IsActive) user.Activate(); else user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetExistingAsync(id, cancellationToken);
        user.ChangePasswordHash(_passwordHasher.Hash(request.NewPassword));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> GetExistingAsync(int id, CancellationToken cancellationToken)
        => await _userRepository.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(User), id);

    private static UserResponse ToResponse(User user) => new(user.Id, user.Email, user.Role.ToString(), user.IsActive);
}
