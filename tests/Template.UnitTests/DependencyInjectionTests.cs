using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Template.Application;
using Template.Application.Abstractions.Messaging;
using Template.Application.Abstractions.Persistence;
using Template.Application.Behaviors;
using Template.Application.Features.Products;

namespace Template.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_ShouldWrapHandlers_InLoggingThenValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(Substitute.For<IProductRepository>());
        services.AddApplication();

        // Act
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProductCommand, Guid>>();

        // Assert
        handler.ShouldBeOfType<LoggingDecorator.CommandHandler<CreateProductCommand, Guid>>();
    }
}
