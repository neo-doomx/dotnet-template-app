using FluentValidation;
using Template.Application.Abstractions.Messaging;
using Template.Domain.Results;

namespace Template.Application.Behaviors;

/// <summary>
/// Runs every FluentValidation validator registered for the request before the handler.
/// A failed validation returns a Validation error and the handler is never called.
/// </summary>
public static class ValidationDecorator
{
    public sealed class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _inner;
        private readonly IEnumerable<IValidator<TCommand>> _validators;

        public CommandHandler(ICommandHandler<TCommand, TResponse> inner, IEnumerable<IValidator<TCommand>> validators)
        {
            _inner = inner;
            _validators = validators;
        }

        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            Error? error = await ValidateAsync(command, _validators, cancellationToken);
            return error ?? await _inner.Handle(command, cancellationToken);
        }
    }

    public sealed class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        private readonly IQueryHandler<TQuery, TResponse> _inner;
        private readonly IEnumerable<IValidator<TQuery>> _validators;

        public QueryHandler(IQueryHandler<TQuery, TResponse> inner, IEnumerable<IValidator<TQuery>> validators)
        {
            _inner = inner;
            _validators = validators;
        }

        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            Error? error = await ValidateAsync(query, _validators, cancellationToken);
            return error ?? await _inner.Handle(query, cancellationToken);
        }
    }

    private static async Task<Error?> ValidateAsync<TRequest>(
        TRequest request,
        IEnumerable<IValidator<TRequest>> validators,
        CancellationToken cancellationToken)
    {
        var context = new ValidationContext<TRequest>(request);

        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (IValidator<TRequest> validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count == 0) return null;

        Dictionary<string, string[]> errors = failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return Error.Validation(errors);
    }
}
