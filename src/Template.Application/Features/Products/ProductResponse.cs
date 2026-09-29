using Template.Domain.Entities;

namespace Template.Application.Features.Products;

public sealed record ProductResponse(Guid Id, string Name, decimal Price, DateTime CreatedAtUtc);

public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.Id, product.Name, product.Price, product.CreatedAtUtc);
}
