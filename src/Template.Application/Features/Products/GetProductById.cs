using Microsoft.Extensions.Caching.Hybrid;
using Template.Application.Abstractions.Messaging;
using Template.Application.Abstractions.Persistence;
using Template.Domain.Results;

namespace Template.Application.Features.Products;

public sealed record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;

public sealed class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ProductResponse>
{
    private readonly IProductRepository _products;
    private readonly HybridCache _cache;

    public GetProductByIdQueryHandler(IProductRepository products, HybridCache cache)
    {
        _products = products;
        _cache = cache;
    }

    public static string CacheKey(Guid id) => $"products:{id}";

    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        // Memory first, then Redis, then SQL Server. The result is written back to both caches.
        ProductResponse? product = await _cache.GetOrCreateAsync(
            CacheKey(query.Id),
            async token => (await _products.GetByIdAsync(query.Id, token))?.ToResponse(),
            cancellationToken: cancellationToken);

        return product is null ? ProductErrors.NotFound(query.Id) : product;
    }
}
