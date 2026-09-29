using Microsoft.AspNetCore.Identity;
using Template.Domain.Entities;

namespace Template.Infrastructure.Security;

/// <summary>
/// PBKDF2 (HMAC-SHA512, 100,000 iterations, random salt) from ASP.NET Core Identity.
/// Verification uses a constant-time comparison.
/// </summary>
public sealed class PasswordHasher : Application.Abstractions.Security.IPasswordHasher
{
    private static readonly PasswordHasher<User> Hasher = new();

    private static readonly string DummyHash = Hasher.HashPassword(null!, Guid.NewGuid().ToString());

    public string Hash(string password) => Hasher.HashPassword(null!, password);

    public bool Verify(string password, string? passwordHash)
    {
        PasswordVerificationResult result = Hasher.VerifyHashedPassword(null!, passwordHash ?? DummyHash, password);
        return passwordHash is not null && result != PasswordVerificationResult.Failed;
    }
}
