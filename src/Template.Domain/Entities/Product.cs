using Template.Domain.Common;

namespace Template.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public decimal Price { get; set; }
}
