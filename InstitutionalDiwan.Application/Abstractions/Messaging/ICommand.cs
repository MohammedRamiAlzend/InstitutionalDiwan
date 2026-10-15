using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Application.Abstractions.Messaging;

/// <summary>Marker interface for a state-changing use case that returns a <see cref="Result"/>.</summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>Marker interface for a state-changing use case that returns a <see cref="Result{TResponse}"/>.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}
