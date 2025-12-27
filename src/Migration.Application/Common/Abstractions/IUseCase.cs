using DMS.Migration.Application.Common.Models;

namespace DMS.Migration.Application.Common.Abstractions;

/// <summary>
/// Marker interface for use cases (commands/queries)
/// </summary>
public interface IUseCase
{
}

/// <summary>
/// Represents a command that performs an action and returns a result
/// </summary>
public interface ICommand<TResponse> : IUseCase
{
}

/// <summary>
/// Represents a query that retrieves data
/// </summary>
public interface IQuery<TResponse> : IUseCase
{
}

/// <summary>
/// Handler for commands
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for queries
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
