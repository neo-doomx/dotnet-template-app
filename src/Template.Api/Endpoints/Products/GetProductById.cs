using Template.Application.Abstractions.Messaging;
using Template.Application.Features.Products;
using Template.Domain.Results;

namespace Template.Api.Endpoints.Products;

public sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("products/{id:guid}", async (
                Guid id,
                IQueryHandler<GetProductByIdQuery, ProductResponse> handler,
                CancellationToken cancellationToken) =>
            {
                Result<ProductResponse> result = await handler.Handle(new GetProductByIdQuery(id), cancellationToken);

                return result.Match(
                    product => Results.Ok(product),
                    error => error.ToProblem());
            })
            .WithTags("Products")
            .WithSummary("Get a product by id (cached in memory and Redis)")
            .Produces<ProductResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization()
            .MapToApiVersion(1);
    }
}
