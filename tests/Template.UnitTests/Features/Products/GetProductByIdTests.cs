using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Abstractions.Persistence;
using Template.Application.Features.Products;
using Template.Domain.Entities;

namespace Template.UnitTests.Features.Products;

public sealed class GetProductByIdTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdTests()
    {
        // Real HybridCache with only the in-memory level, no Redis.
        HybridCache cache = new ServiceCollection()
            .AddHybridCache().Services
            .BuildServiceProvider()
            .GetRequiredService<HybridCache>();

        _handler = new GetProductByIdQueryHandler(_products, cache);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        _products.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Product?)null);

        // Act
        var result = await _handler.Handle(new GetProductByIdQuery(id), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.NotFound(id));
    }

    [Fact]
    public async Task Handle_ShouldHitDatabaseOnce_WhenCalledTwice()
    {
        // Arrange
        var product = new Product { Id = Guid.NewGuid(), Name = "Keyboard", Price = 49.99m };
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        // Act
        var first = await _handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);
        var second = await _handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        // Assert
        first.Value.ShouldBe(product.ToResponse());
        second.Value.ShouldBe(product.ToResponse());
        await _products.Received(1).GetByIdAsync(product.Id, Arg.Any<CancellationToken>());
    }
}
