using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Application.Abstractions.Messaging;

/// <summary>Marker interface for a read-only use case that returns a <see cref="Result{TResponse}"/>.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
