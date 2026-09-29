using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Template.Application.Abstractions.Messaging;
using Template.Domain.Results;

namespace Template.Application.Behaviors;

/// <summary>
/// Logs the request name, outcome and duration. The request body is never logged,
/// because commands such as AuthenticateUserQuery carry passwords.
/// </summary>
public static class LoggingDecorator
{
    public sealed class CommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private readonly ICommandHandler<TCommand, TResponse> _inner;
        private readonly ILogger<CommandHandler<TCommand, TResponse>> _logger;

        public CommandHandler(ICommandHandler<TCommand, TResponse> inner,
            ILogger<CommandHandler<TCommand, TResponse>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken) =>
            LogAsync(typeof(TCommand).Name, () => _inner.Handle(command, cancellationToken), _logger);
    }

    public sealed class QueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        private readonly IQueryHandler<TQuery, TResponse> _inner;
        private readonly ILogger<QueryHandler<TQuery, TResponse>> _logger;

        public QueryHandler(IQueryHandler<TQuery, TResponse> inner, ILogger<QueryHandler<TQuery, TResponse>> logger)
        {
            _inner = inner;
            _logger = logger;
        }

        public Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken) =>
            LogAsync(typeof(TQuery).Name, () => _inner.Handle(query, cancellationToken), _logger);
    }

    private static async Task<Result<TResponse>> LogAsync<TResponse>(
        string requestName,
        Func<Task<Result<TResponse>>> next,
        ILogger logger)
    {
        long start = Stopwatch.GetTimestamp();

        Result<TResponse> result = await next();

        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        if (result.IsSuccess)
        {
            logger.LogInformation("Handled {RequestName} in {ElapsedMs:0} ms", requestName, elapsedMs);
        }
        else
        {
            logger.LogWarning("{RequestName} failed with {ErrorCode} in {ElapsedMs:0} ms",
                requestName, result.Error.Code, elapsedMs);
        }

        return result;
    }
}
