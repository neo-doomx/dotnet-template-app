using Template.Application.Abstractions.Persistence;
using Template.Application.Features.Products;
using Template.Domain.Entities;

namespace Template.UnitTests.Features.Products;

public sealed class CreateProductTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly CreateProductCommandHandler _handler;

    public CreateProductTests()
    {
        _handler = new CreateProductCommandHandler(_products);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenNameIsTaken()
    {
        // Arrange
        _products.NameExistsAsync("Keyboard", Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _handler.Handle(new CreateProductCommand("Keyboard", 49.99m), CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(ProductErrors.NameTaken);
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAddTrimmedProduct_WhenNameIsFree()
    {
        // Arrange
        _products.NameExistsAsync("Keyboard", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(new CreateProductCommand("  Keyboard  ", 49.99m), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _products.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Id == result.Value && p.Name == "Keyboard" && p.Price == 49.99m),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", 10)]
    [InlineData("Keyboard", 0)]
    [InlineData("Keyboard", -1)]
    [InlineData("Keyboard", 10.999)]
    public void Validator_ShouldFail_WhenCommandIsInvalid(string name, double price)
    {
        var validator = new CreateProductCommandValidator();

        var result = validator.Validate(new CreateProductCommand(name, (decimal)price));

        result.IsValid.ShouldBeFalse();
    }
}
