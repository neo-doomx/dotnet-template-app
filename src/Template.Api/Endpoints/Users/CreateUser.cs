using Template.Application.Abstractions.Messaging;
using Template.Application.Features.Users;
using Template.Domain.Results;

namespace Template.Api.Endpoints.Users;

public sealed record CreateUserRequest(string Username, string Password)
{
    public CreateUserCommand ToCommand() => new(Username, Password);
}

public sealed record CreateUserResponse(Guid Id);

public sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users", async (
                CreateUserRequest request,
                ICommandHandler<CreateUserCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                Result<Guid> result = await handler.Handle(request.ToCommand(), cancellationToken);

                return result.Match(
                    id => Results.Created(string.Empty, new CreateUserResponse(id)),
                    error => error.ToProblem());
            })
            .WithTags("Users")
            .WithSummary("Register a user for Basic authentication")
            .Produces<CreateUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .AllowAnonymous()
            .MapToApiVersion(1);
    }
}
