using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

internal sealed class CreateSchemaDraftCommandHandler
    : ICommandHandler<CreateSchemaDraftCommand, SchemaVersionResponse>
{
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateSchemaDraftCommandHandler(
        IDocumentTypeRepository documentTypeRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IUnitOfWork unitOfWork)
    {
        _documentTypeRepository = documentTypeRepository;
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SchemaVersionResponse>> Handle(
        CreateSchemaDraftCommand request,
        CancellationToken cancellationToken)
    {
        if (await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken) is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        if (await _versionRepository.GetDraftAsync(request.DocumentTypeId, cancellationToken) is not null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.SchemaVersionAlreadyHasDraft);

        var versionNumber = await _versionRepository.GetLatestVersionNumberAsync(
            request.DocumentTypeId, cancellationToken) + 1;
        var published = await _versionRepository.GetPublishedAsync(request.DocumentTypeId, cancellationToken);

        var draftResult = published is null
            ? DocumentTypeSchemaVersion.CreateDraft(
                request.DocumentTypeId, versionNumber, request.UserId, _unitOfWork.UtcNow)
            : DocumentTypeSchemaVersion.CreateDraftFrom(
                published, versionNumber, request.UserId, _unitOfWork.UtcNow);
        if (draftResult.IsFailure)
            return Result.Failure<SchemaVersionResponse>(draftResult.Error);

        _versionRepository.Add(draftResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SchemaVersionResponse.From(draftResult.Value);
    }
}

internal sealed class DiscardSchemaDraftCommandHandler : ICommandHandler<DiscardSchemaDraftCommand>
{
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DiscardSchemaDraftCommandHandler(
        IDocumentTypeSchemaVersionRepository versionRepository,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DiscardSchemaDraftCommand request, CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionNotFound);

        // Only a draft is ever discarded: documents are pinned to published versions, never to a draft.
        if (version.Status != DocumentTypeSchemaVersionStatus.Draft)
            return Result.Failure(DomainErrors.DocumentFields.SchemaVersionNotDraft);

        _versionRepository.Remove(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

internal sealed class PublishSchemaVersionCommandHandler
    : ICommandHandler<PublishSchemaVersionCommand, SchemaVersionResponse>
{
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishSchemaVersionCommandHandler(
        IDocumentTypeSchemaVersionRepository versionRepository,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SchemaVersionResponse>> Handle(
        PublishSchemaVersionCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.SchemaVersionNotFound);

        // Read before publishing, so "the current version" cannot be the draft we are about to publish.
        var previous = await _versionRepository.GetPublishedAsync(version.DocumentTypeId, cancellationToken);

        var published = version.Publish(request.UserId, _unitOfWork.UtcNow);
        if (published.IsFailure)
            return Result.Failure<SchemaVersionResponse>(published.Error);

        // The version that was current until now is superseded; its pinned documents keep using it.
        if (previous is not null && previous.SchemaVersionId != version.SchemaVersionId)
        {
            var retired = previous.Retire(_unitOfWork.UtcNow);
            if (retired.IsFailure)
                return Result.Failure<SchemaVersionResponse>(retired.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SchemaVersionResponse.From(version);
    }
}
