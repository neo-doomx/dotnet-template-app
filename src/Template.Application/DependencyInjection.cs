using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Template.Application.Abstractions.Messaging;
using Template.Application.Behaviors;

namespace Template.Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        // Register every command and query handler. Open generic types are the decorators, so skip them.
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(classes => classes
                .AssignableTo(typeof(ICommandHandler<,>))
                .Where(type => !type.IsGenericTypeDefinition), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes
                .AssignableTo(typeof(IQueryHandler<,>))
                .Where(type => !type.IsGenericTypeDefinition), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        // Pipeline order: the last decorator registered runs first. Logging -> Validation -> Handler.
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationDecorator.QueryHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
    }
}
