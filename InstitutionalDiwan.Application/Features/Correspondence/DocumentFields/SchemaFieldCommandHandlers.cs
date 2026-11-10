using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;

internal sealed class CreateDocumentFieldCommandHandler
    : ICommandHandler<CreateDocumentFieldCommand, DocumentFieldResponse>
{
    private readonly IDocumentTypeRepository _documentTypeRepository;
    private readonly IDocumentFieldRepository _fieldRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentFieldCommandHandler(
        IDocumentTypeRepository documentTypeRepository,
        IDocumentFieldRepository fieldRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _documentTypeRepository = documentTypeRepository;
        _fieldRepository = fieldRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentFieldResponse>> Handle(
        CreateDocumentFieldCommand request,
        CancellationToken cancellationToken)
    {
        if (await _documentTypeRepository.GetByIdAsync(request.DocumentTypeId, cancellationToken) is null)
            return Result.Failure<DocumentFieldResponse>(DomainErrors.Correspondence.DocumentTypeNotFound);

        if (request.DepartmentId is { } departmentId
            && await _departmentRepository.GetByIdAsync(departmentId, cancellationToken) is null)
        {
            return Result.Failure<DocumentFieldResponse>(DomainErrors.Department.NotFound);
        }

        var key = request.Key?.Trim() ?? string.Empty;
        if (await _fieldRepository.GetByKeyAsync(request.DocumentTypeId, request.DepartmentId, key, cancellationToken) is not null)
            return Result.Failure<DocumentFieldResponse>(DomainErrors.DocumentFields.KeyAlreadyExists);

        var fieldResult = DocumentField.Create(
            request.DocumentTypeId, key, request.DataType, request.UserId, _unitOfWork.UtcNow, request.DepartmentId);
        if (fieldResult.IsFailure)
            return Result.Failure<DocumentFieldResponse>(fieldResult.Error);

        _fieldRepository.Add(fieldResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentFieldResponse.From(fieldResult.Value);
    }
}

internal sealed class AddFieldToDraftCommandHandler
    : ICommandHandler<AddFieldToDraftCommand, SchemaVersionResponse>
{
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldRepository _fieldRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IUnitOfWork _unitOfWork;

    public AddFieldToDraftCommandHandler(
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldRepository fieldRepository,
        IDocumentFieldOptionResolver optionResolver,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _fieldRepository = fieldRepository;
        _optionResolver = optionResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SchemaVersionResponse>> Handle(
        AddFieldToDraftCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.SchemaVersionNotFound);

        var field = await _fieldRepository.GetByIdAsync(request.DocumentFieldId, cancellationToken);
        if (field is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.FieldNotFound);

        var settings = request.Settings.ToSettings(field.DataType);
        if (settings.IsFailure)
            return Result.Failure<SchemaVersionResponse>(settings.Error);

        var source = _optionResolver.EnsureSourceSupported(settings.Value.Behavior);
        if (source.IsFailure)
            return Result.Failure<SchemaVersionResponse>(source.Error);

        var added = version.AddField(field, settings.Value, _unitOfWork.UtcNow);
        if (added.IsFailure)
            return Result.Failure<SchemaVersionResponse>(added.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SchemaVersionResponse.From(version);
    }
}

internal sealed class UpdateDraftFieldCommandHandler
    : ICommandHandler<UpdateDraftFieldCommand, SchemaVersionResponse>
{
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDocumentFieldOptionResolver _optionResolver;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDraftFieldCommandHandler(
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDocumentFieldOptionResolver optionResolver,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _optionResolver = optionResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SchemaVersionResponse>> Handle(
        UpdateDraftFieldCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.SchemaVersionNotFound);

        var existing = version.Fields.FirstOrDefault(field => field.DocumentFieldId == request.DocumentFieldId);
        if (existing is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.FieldNotInVersion);

        var settings = request.Settings.ToSettings(existing.DataType);
        if (settings.IsFailure)
            return Result.Failure<SchemaVersionResponse>(settings.Error);

        var source = _optionResolver.EnsureSourceSupported(settings.Value.Behavior);
        if (source.IsFailure)
            return Result.Failure<SchemaVersionResponse>(source.Error);

        var updated = version.UpdateField(request.DocumentFieldId, settings.Value, _unitOfWork.UtcNow);
        if (updated.IsFailure)
            return Result.Failure<SchemaVersionResponse>(updated.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SchemaVersionResponse.From(version);
    }
}

internal sealed class RemoveDraftFieldCommandHandler
    : ICommandHandler<RemoveDraftFieldCommand, SchemaVersionResponse>
{
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveDraftFieldCommandHandler(
        IDocumentTypeSchemaVersionRepository versionRepository,
        IUnitOfWork unitOfWork)
    {
        _versionRepository = versionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SchemaVersionResponse>> Handle(
        RemoveDraftFieldCommand request,
        CancellationToken cancellationToken)
    {
        var version = await _versionRepository.GetByIdAsync(request.SchemaVersionId, cancellationToken);
        if (version is null)
            return Result.Failure<SchemaVersionResponse>(DomainErrors.DocumentFields.SchemaVersionNotFound);

        var removed = version.RemoveField(request.DocumentFieldId, _unitOfWork.UtcNow);
        if (removed.IsFailure)
            return Result.Failure<SchemaVersionResponse>(removed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SchemaVersionResponse.From(version);
    }
}
