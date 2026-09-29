using Template.Application.Abstractions.Persistence;
using Template.Application.Abstractions.Security;
using Template.Application.Features.Users;
using Template.Domain.Entities;

namespace Template.UnitTests.Features.Users;

public sealed class AuthenticateUserTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly AuthenticateUserQueryHandler _handler;

    private readonly User _alice = new() { Id = Guid.NewGuid(), Username = "alice", PasswordHash = "hashed" };

    public AuthenticateUserTests()
    {
        _handler = new AuthenticateUserQueryHandler(_users, _passwordHasher);
    }

    [Fact]
    public async Task Handle_ShouldStillVerifyPassword_WhenUserDoesNotExist()
    {
        // Arrange
        _users.GetByUsernameAsync("bob", Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(new AuthenticateUserQuery("bob", "password"), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(UserErrors.InvalidCredentials);
        _passwordHasher.Received(1).Verify("password", null);
    }

    [Fact]
    public async Task Handle_ShouldReturnInvalidCredentials_WhenPasswordIsWrong()
    {
        // Arrange
        _users.GetByUsernameAsync("alice", Arg.Any<CancellationToken>()).Returns(_alice);
        _passwordHasher.Verify("wrong", "hashed").Returns(false);

        // Act
        var result = await _handler.Handle(new AuthenticateUserQuery("alice", "wrong"), CancellationToken.None);

        // Assert
        result.Error.ShouldBe(UserErrors.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_ShouldReturnUser_WhenCredentialsAreValid()
    {
        // Arrange
        _users.GetByUsernameAsync("alice", Arg.Any<CancellationToken>()).Returns(_alice);
        _passwordHasher.Verify("correct", "hashed").Returns(true);

        // Act
        var result = await _handler.Handle(new AuthenticateUserQuery(" Alice ", "correct"), CancellationToken.None);

        // Assert
        result.Value.ShouldBe(new AuthenticatedUser(_alice.Id, "alice"));
    }
}
