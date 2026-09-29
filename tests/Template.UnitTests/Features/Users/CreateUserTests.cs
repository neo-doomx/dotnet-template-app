using Template.Application.Abstractions.Persistence;
using Template.Application.Abstractions.Security;
using Template.Application.Features.Users;
using Template.Domain.Entities;

namespace Template.UnitTests.Features.Users;

public sealed class CreateUserTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserTests()
    {
        _handler = new CreateUserCommandHandler(_users, _passwordHasher);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenUsernameIsTaken()
    {
        // Arrange
        _users.UsernameExistsAsync("alice", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new CreateUserCommand("Alice", "a-long-password"), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.UsernameTaken);
    }

    [Fact]
    public async Task Handle_ShouldStoreHashedPassword_WhenUsernameIsFree()
    {
        // Arrange
        _passwordHasher.Hash("a-long-password").Returns("hashed");

        // Act
        var result = await _handler.Handle(new CreateUserCommand("Alice", "a-long-password"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Username == "alice" && u.PasswordHash == "hashed"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("al", "a-long-password")]
    [InlineData("alice:admin", "a-long-password")]
    [InlineData("alice", "short")]
    public void Validator_ShouldFail_WhenCommandIsInvalid(string username, string password)
    {
        var validator = new CreateUserCommandValidator();

        var result = validator.Validate(new CreateUserCommand(username, password));

        result.IsValid.ShouldBeFalse();
    }
}
