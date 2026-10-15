using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Application.Abstractions.Messaging;

/// <summary>Handles an <see cref="ICommand"/> and returns a <see cref="Result"/>.</summary>
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>Handles an <see cref="ICommand{TResponse}"/> and returns a <see cref="Result{TResponse}"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
