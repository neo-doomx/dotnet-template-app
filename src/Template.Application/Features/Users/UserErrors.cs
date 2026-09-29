using Template.Domain.Results;

namespace Template.Application.Features.Users;

public static class UserErrors
{
    public static readonly Error UsernameTaken =
        Error.Conflict("Users.UsernameTaken", "This username is already taken.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("Users.InvalidCredentials", "Invalid username or password.");
}
