using FluentValidation;
using Template.Application.Abstractions.Messaging;
using Template.Application.Behaviors;
using Template.Application.Features.Products;
using Template.Domain.Results;

namespace Template.UnitTests.Behaviors;

public sealed class ValidationDecoratorTests
{
    private readonly ICommandHandler<CreateProductCommand, Guid> _inner =
        Substitute.For<ICommandHandler<CreateProductCommand, Guid>>();

    private readonly ValidationDecorator.CommandHandler<CreateProductCommand, Guid> _decorator;

    public ValidationDecoratorTests()
    {
        IValidator<CreateProductCommand>[] validators = [new CreateProductCommandValidator()];
        _decorator = new ValidationDecorator.CommandHandler<CreateProductCommand, Guid>(_inner, validators);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_AndSkipHandler_WhenCommandIsInvalid()
    {
        // Act
        var result = await _decorator.Handle(new CreateProductCommand("", 0), CancellationToken.None);

        // Assert
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.ValidationErrors.Keys.ShouldBe(["Name", "Price"], ignoreOrder: true);
        await _inner.DidNotReceive().Handle(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCallHandler_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProductCommand("Keyboard", 49.99m);
        var id = Guid.NewGuid();
        _inner.Handle(command, Arg.Any<CancellationToken>()).Returns(id);

        // Act
        var result = await _decorator.Handle(command, CancellationToken.None);

        // Assert
        result.Value.ShouldBe(id);
    }
}
