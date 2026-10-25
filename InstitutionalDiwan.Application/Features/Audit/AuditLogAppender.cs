using InstitutionalDiwan.Application.Abstractions.Context;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Audit;

/// <summary>
/// Appends an audit entry to the end of the hash chain. It is deliberately not responsible for
/// saving: the caller adds the entry to the same unit of work as the action it describes, so the
/// two rows commit atomically.
/// </summary>
internal sealed class AuditLogAppender
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestContext _requestContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditLogAppender(
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IRequestContext requestContext,
        IDateTimeProvider dateTimeProvider)
    {
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _requestContext = requestContext;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <summary>Appends an entry using the current request's correlation id and client address.</summary>
    public Task<Result<AuditLog>> AppendAsync(
        string moduleName,
        int actionTypeId,
        int entityTypeId,
        string entityId,
        Guid? actorUserId,
        Guid? actorDepartmentId = null,
        Guid? relatedDocumentId = null,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        CancellationToken cancellationToken = default) =>
        AppendAsync(
            moduleName,
            actionTypeId,
            entityTypeId,
            entityId,
            actorUserId,
            actorDepartmentId,
            relatedDocumentId,
            oldValuesJson,
            newValuesJson,
            _requestContext.IpAddress,
            _requestContext.CorrelationId,
            cancellationToken);

    public async Task<Result<AuditLog>> AppendAsync(
        string moduleName,
        int actionTypeId,
        int entityTypeId,
        string entityId,
        Guid? actorUserId,
        Guid? actorDepartmentId,
        Guid? relatedDocumentId,
        string? oldValuesJson,
        string? newValuesJson,
        string? ipAddress,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        var latest = await _auditLogRepository.GetLatestAsync(cancellationToken);
        var previousHash = latest?.EventHash ?? AuditLog.GenesisHash;

        var result = AuditLog.Record(
            moduleName,
            actionTypeId,
            entityTypeId,
            entityId,
            previousHash,
            actorUserId,
            actorDepartmentId,
            relatedDocumentId,
            oldValuesJson,
            newValuesJson,
            ipAddress,
            correlationId,
            _dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return result;

        _auditLogRepository.Add(result.Value);
        return result;
    }
}
