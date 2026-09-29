using FluentValidation;
using Template.Application.Abstractions.Messaging;
using Template.Application.Abstractions.Persistence;
using Template.Domain.Entities;
using Template.Domain.Results;

namespace Template.Application.Features.Products;

public sealed record CreateProductCommand(string Name, decimal Price) : ICommand<Guid>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _products;

    public CreateProductCommandHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        string name = command.Name.Trim();

        if (await _products.NameExistsAsync(name, cancellationToken))
        {
            return ProductErrors.NameTaken;
        }

        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Price = command.Price
        };

        await _products.AddAsync(product, cancellationToken);

        return product.Id;
    }
}
