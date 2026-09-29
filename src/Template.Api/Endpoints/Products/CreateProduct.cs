using Template.Application.Abstractions.Messaging;
using Template.Application.Features.Products;
using Template.Domain.Results;

namespace Template.Api.Endpoints.Products;

public sealed record CreateProductRequest(string Name, decimal Price)
{
    public CreateProductCommand ToCommand() => new(Name, Price);
}

public sealed record CreateProductResponse(Guid Id);

public sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("products", async (
                CreateProductRequest request,
                ICommandHandler<CreateProductCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await handler.Handle(request.ToCommand(), cancellationToken);

                return result.Match(
                    id => Results.Created($"/api/v1/products/{id}", new CreateProductResponse(id)),
                    error => error.ToProblem());
            })
            .WithTags("Products")
            .WithSummary("Create a product")
            .Produces<CreateProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization()
            .MapToApiVersion(1);
    }
}
