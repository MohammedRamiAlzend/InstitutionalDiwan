using FluentValidation;
using InstitutionalDiwan.Domain.Common;
using MediatR;

namespace InstitutionalDiwan.Application.Behaviors;

/// <summary>
/// Runs every registered <see cref="IValidator{T}"/> before the handler and short-circuits the pipeline
/// with a failed <see cref="Result"/> (never an exception) when validation fails.
/// </summary>
public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators) =>
        _validators = validators;

    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(validator => validator.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count == 0)
            return next();

        return Task.FromResult(BuildFailure(failures));
    }

    private static TResponse BuildFailure(IReadOnlyCollection<FluentValidation.Results.ValidationFailure> failures)
    {
        var error = Error.Validation(
            failures.First().ErrorCode is { Length: > 0 } code ? code : "Validation.Failed",
            string.Join(" ", failures.Select(failure => failure.ErrorMessage)));

        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var failureFactory = typeof(Result)
            .GetMethods()
            .First(method => method.Name == nameof(Result.Failure)
                             && method.IsGenericMethodDefinition
                             && method.GetParameters().Length == 1);

        var result = failureFactory.MakeGenericMethod(valueType).Invoke(null, new object[] { error })!;
        return (TResponse)result;
    }
}
