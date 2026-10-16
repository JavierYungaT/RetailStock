using FluentAssertions;
using Moq;
using RetailStock.Application.Auth;
using RetailStock.Application.Common.Interfaces;
using RetailStock.Application.Users;
using RetailStock.Domain.Entities;
using RetailStock.Domain.Enums;
using RetailStock.Domain.Exceptions;
using RetailStock.Tests.TestHelpers;

namespace RetailStock.Tests.Application;

/// <summary>Reglas que impiden que el sistema se quede sin administración.</summary>
public class UserServiceTests
{
    private const int CurrentAdminId = 1;

    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(CurrentAdminId);
        _passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hash");
        _sut = new UserService(_userRepository.Object, _passwordHasher.Object, _currentUser.Object, _unitOfWork.Object);
    }

    [Fact]
    public async Task UpdateAsync_AdminDeactivatesHimself_ThrowsBusinessRuleException()
    {
        // Arrange
        GivenUser(new User("admin@x.com", "h", UserRole.Admin).WithId(CurrentAdminId));

        // Act
        var act = () => _sut.UpdateAsync(CurrentAdminId, new UpdateUserRequest { Role = UserRole.Admin, IsActive = false });

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DemotingTheLastActiveAdmin_ThrowsConflictException()
    {
        // Arrange: otro admin (id 2) y es el único activo
        GivenUser(new User("otro@x.com", "h", UserRole.Admin).WithId(2));
        _userRepository.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act
        var act = () => _sut.UpdateAsync(2, new UpdateUserRequest { Role = UserRole.Cashier, IsActive = true });

        // Assert
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*al menos un administrador*");
    }

    [Fact]
    public async Task UpdateAsync_DeactivateCashier_SavesAndReturnsInactiveUser()
    {
        // Arrange
        var cashier = new User("caja@x.com", "h", UserRole.Cashier).WithId(3);
        GivenUser(cashier);

        // Act
        var result = await _sut.UpdateAsync(3, new UpdateUserRequest { Role = UserRole.Cashier, IsActive = false });

        // Assert
        result.IsActive.Should().BeFalse();
        cashier.IsActive.Should().BeFalse();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>()), Times.Never, "no es admin: no hace falta contar");
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        _userRepository.Setup(r => r.EmailExistsAsync("caja@x.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        var act = () => _sut.CreateAsync(new RegisterRequest { Email = " CAJA@x.com ", Password = "Caja123!", Role = UserRole.Cashier });

        // Assert: el email se normaliza antes de buscar duplicados
        await act.Should().ThrowAsync<ConflictException>();
        _userRepository.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
    }

    private void GivenUser(User user)
        => _userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
}
