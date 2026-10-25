using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Persistence;
using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Application.Behaviors;

/// <summary>
/// Wraps commands in one explicit transaction. The aggregate change and the audit rows the
/// interceptor appends during <c>SaveChangesAsync</c> therefore commit together; a failed result or
/// an exception rolls everything back.
/// </summary>
public sealed class TransactionPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ITransactionManager _transactionManager;

    public TransactionPipelineBehavior(ITransactionManager transactionManager) =>
        _transactionManager = transactionManager;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
            return await next();

        await using var transaction = await _transactionManager
            .BeginTransactionAsync(cancellationToken);

        var response = await next();

        if (response is Result { IsFailure: true })
            return response;

        await transaction.CommitAsync(cancellationToken);
        return response;
    }

    private static bool IsCommand() =>
        typeof(ICommand).IsAssignableFrom(typeof(TRequest)) ||
        typeof(TRequest).GetInterfaces().Any(type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICommand<>));
}
