using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Application.Abstractions.Messaging;

/// <summary>Handles an <see cref="IQuery{TResponse}"/> and returns a <see cref="Result{TResponse}"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
