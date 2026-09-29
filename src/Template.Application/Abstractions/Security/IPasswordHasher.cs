namespace Template.Application.Abstractions.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a stored hash. When <paramref name="passwordHash"/> is null
    /// the password is still hashed against a dummy value, so an unknown username takes the
    /// same time to reject as a wrong password.
    /// </summary>
    bool Verify(string password, string? passwordHash);
}
