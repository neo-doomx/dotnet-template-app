using Template.Application.Abstractions.Messaging;
using Template.Application.Abstractions.Persistence;
using Template.Application.Abstractions.Security;
using Template.Domain.Entities;
using Template.Domain.Results;

namespace Template.Application.Features.Users;

public sealed record AuthenticateUserQuery(string Username, string Password) : IQuery<AuthenticatedUser>;

public sealed record AuthenticatedUser(Guid Id, string Username);

public sealed class AuthenticateUserQueryHandler : IQueryHandler<AuthenticateUserQuery, AuthenticatedUser>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;

    public AuthenticateUserQueryHandler(IUserRepository users, IPasswordHasher passwordHasher)
    {
        _users = users;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<AuthenticatedUser>> Handle(AuthenticateUserQuery query,
        CancellationToken cancellationToken)
    {
        User? user = await _users.GetByUsernameAsync(query.Username.Trim().ToLowerInvariant(), cancellationToken);

        // Always verify, even for unknown users, so response time does not reveal which usernames exist.
        bool isValid = _passwordHasher.Verify(query.Password, user?.PasswordHash);

        if (user is null || !isValid)
        {
            return UserErrors.InvalidCredentials;
        }

        return new AuthenticatedUser(user.Id, user.Username);
    }
}
