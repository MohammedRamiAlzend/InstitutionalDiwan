using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Context;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Application.Behaviors;

/// <summary>
/// Writes one structured start/end (or failure) record around every request. The user id and
/// correlation id are logged as structured properties so they line up with the audit trail and the
/// HTTP request log.
/// </summary>
public sealed class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly IUserContext _userContext;
    private readonly IRequestContext _requestContext;

    public LoggingPipelineBehavior(
        ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger,
        IUserContext userContext,
        IRequestContext requestContext)
    {
        _logger = logger;
        _userContext = userContext;
        _requestContext = requestContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _userContext.IsAuthenticated ? _userContext.UserId : (Guid?)null;
        var correlationId = _requestContext.CorrelationId;

        _logger.LogInformation(
            "Handling {RequestName} for {UserId} with correlation {CorrelationId}",
            requestName,
            userId,
            correlationId);

        try
        {
            var response = await next();

            _logger.LogInformation(
                "Handled {RequestName} for {UserId} with correlation {CorrelationId}",
                requestName,
                userId,
                correlationId);

            return response;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed {RequestName} for {UserId} with correlation {CorrelationId}",
                requestName,
                userId,
                correlationId);
            throw;
        }
    }
}
