using System.Runtime.CompilerServices;
using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// Stand-in for <see cref="ISender"/> used where a handler dispatches follow-up commands
/// (department register line, movement). Every request is recorded; each succeeds with an empty
/// response unless <see cref="FailWith"/> is set for its request type.
/// </summary>
internal sealed class FakeSender : ISender
{
    public List<object> Requests { get; } = new();

    /// <summary>Request types that should fail, with the error to return.</summary>
    public Dictionary<Type, Error> FailWith { get; } = new();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);

        if (FailWith.TryGetValue(request.GetType(), out var error))
            return Task.FromResult(CreateFailure<TResponse>(error));

        return Task.FromResult(CreateSuccess<TResponse>());
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        Requests.Add(request);
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult<object?>(null);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
        IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    private static TResponse CreateSuccess<TResponse>()
    {
        // TResponse is Result<TValue>; build a success wrapping an uninitialised TValue.
        var valueType = typeof(TResponse).GetGenericArguments().Single();
        var value = valueType.IsValueType ? Activator.CreateInstance(valueType) : RuntimeHelpers.GetUninitializedObject(valueType);
        var method = typeof(Result).GetMethod(nameof(Result.Success), 1, new[] { Type.MakeGenericMethodParameter(0) })!;
        return (TResponse)method.MakeGenericMethod(valueType).Invoke(null, new[] { value })!;
    }

    private static TResponse CreateFailure<TResponse>(Error error)
    {
        var valueType = typeof(TResponse).GetGenericArguments().Single();
        var method = typeof(Result).GetMethod(nameof(Result.Failure), 1, new[] { typeof(Error) })!;
        return (TResponse)method.MakeGenericMethod(valueType).Invoke(null, new object[] { error })!;
    }
}
