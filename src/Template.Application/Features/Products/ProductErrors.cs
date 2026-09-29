using Template.Domain.Results;

namespace Template.Application.Features.Products;

public static class ProductErrors
{
    public static readonly Error NameTaken =
        Error.Conflict("Products.NameTaken", "A product with this name already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Products.NotFound", $"Product '{id}' was not found.");
}
