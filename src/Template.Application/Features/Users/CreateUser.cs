using FluentValidation;
using Template.Application.Abstractions.Messaging;
using Template.Application.Abstractions.Persistence;
using Template.Application.Abstractions.Security;
using Template.Domain.Entities;
using Template.Domain.Results;

namespace Template.Application.Features.Users;

public sealed record CreateUserCommand(string Username, string Password) : ICommand<Guid>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        // No ':' allowed, because Basic authentication splits "username:password" on the first colon.
        RuleFor(x => x.Username)
            .NotEmpty()
            .Length(3, 50)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128);
    }
}

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        string username = command.Username.Trim().ToLowerInvariant();

        if (await _users.UsernameExistsAsync(username, cancellationToken))
        {
            return UserErrors.UsernameTaken;
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = username,
            PasswordHash = _passwordHasher.Hash(command.Password)
        };

        await _users.AddAsync(user, cancellationToken);

        return user.Id;
    }
}
