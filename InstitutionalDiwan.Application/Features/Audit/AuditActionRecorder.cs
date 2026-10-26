using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Application.Features.Audit;

internal sealed class AuditActionRecorder : IAuditActionRecorder
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditActionRecorder> _logger;

    public AuditActionRecorder(
        IServiceScopeFactory scopeFactory,
        ILogger<AuditActionRecorder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RecordAsync(
        AuditActionRecord record,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var appender = scope.ServiceProvider.GetRequiredService<AuditLogAppender>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var appendResult = await appender.AppendAsync(
                record.ModuleName,
                record.ActionTypeId,
                record.EntityTypeId,
                record.EntityId,
                record.ActorUserId,
                record.ActorDepartmentId,
                relatedDocumentId: null,
                oldValuesJson: null,
                newValuesJson: record.NewValuesJson,
                cancellationToken: cancellationToken);

            if (appendResult.IsFailure)
            {
                _logger.LogWarning(
                    "Audit action {EntityId} was not recorded: {Code}.",
                    record.EntityId,
                    appendResult.Error.Code);
                return;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            // The action already completed. A failed audit write must not change its outcome.
            _logger.LogError(
                exception,
                "Could not record the audit action {EntityId}.",
                record.EntityId);
        }
    }
}
