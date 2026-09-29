using Template.Domain.Common;

namespace Template.Domain.Entities;

public sealed class User : AuditableEntity
{
    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string PasswordHash { get; set; }
}
