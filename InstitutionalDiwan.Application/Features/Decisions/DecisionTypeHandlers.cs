using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Decisions;

internal sealed class CreateDecisionTypeCommandHandler
    : ICommandHandler<CreateDecisionTypeCommand, DecisionTypeResponse>
{
    private readonly IDecisionTypeRepository _types;
    private readonly IDocumentTypeRepository _documentTypes;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDecisionTypeCommandHandler(
        IDecisionTypeRepository types,
        IDocumentTypeRepository documentTypes,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork)
    {
        _types = types;
        _documentTypes = documentTypes;
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DecisionTypeResponse>> Handle(
        CreateDecisionTypeCommand request,
        CancellationToken cancellationToken)
    {
        var destination = await DecisionTypeChecks.DestinationAsync(
            _departments, request.EffectId, request.DestinationDepartmentId, requireActive: true, cancellationToken);
        if (destination.IsFailure)
            return Result.Failure<DecisionTypeResponse>(destination.Error);

        var documentTypes = await DecisionTypeChecks.DocumentTypesAsync(
            _documentTypes, request.DocumentTypeIds, cancellationToken);
        if (documentTypes.IsFailure)
            return Result.Failure<DecisionTypeResponse>(documentTypes.Error);

        var created = DecisionType.Create(
            request.NameAr,
            request.IsAppealable,
            request.EffectId,
            destination.Value,
            documentTypes.Value,
            isActive: request.EffectId != DecisionEffect.ReferId || destination.Value is not null,
            utcNow: _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<DecisionTypeResponse>(created.Error);

        _types.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(created.Value));
    }
}

internal sealed class UpdateDecisionTypeCommandHandler
    : ICommandHandler<UpdateDecisionTypeCommand, DecisionTypeResponse>
{
    private readonly IDecisionTypeRepository _types;
    private readonly IDocumentTypeRepository _documentTypes;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDecisionTypeCommandHandler(
        IDecisionTypeRepository types,
        IDocumentTypeRepository documentTypes,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork)
    {
        _types = types;
        _documentTypes = documentTypes;
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DecisionTypeResponse>> Handle(
        UpdateDecisionTypeCommand request,
        CancellationToken cancellationToken)
    {
        var type = await _types.GetByIdAsync(request.DecisionTypeId, cancellationToken);
        if (type is null)
            return Result.Failure<DecisionTypeResponse>(DomainErrors.Decisions.TypeNotFound);

        var destination = await DecisionTypeChecks.DestinationAsync(
            _departments, request.EffectId, request.DestinationDepartmentId, requireActive: true, cancellationToken);
        if (destination.IsFailure)
            return Result.Failure<DecisionTypeResponse>(destination.Error);

        var documentTypes = await DecisionTypeChecks.DocumentTypesAsync(
            _documentTypes, request.DocumentTypeIds, cancellationToken);
        if (documentTypes.IsFailure)
            return Result.Failure<DecisionTypeResponse>(documentTypes.Error);

        var updated = type.Update(
            request.NameAr,
            request.IsAppealable,
            request.EffectId,
            destination.Value,
            documentTypes.Value,
            _unitOfWork.UtcNow);
        if (updated.IsFailure)
            return Result.Failure<DecisionTypeResponse>(updated.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(type));
    }
}

internal sealed class ActivateDecisionTypeCommandHandler
    : ICommandHandler<ActivateDecisionTypeCommand, DecisionTypeResponse>
{
    private readonly IDecisionTypeRepository _types;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateDecisionTypeCommandHandler(
        IDecisionTypeRepository types,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork)
    {
        _types = types;
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DecisionTypeResponse>> Handle(
        ActivateDecisionTypeCommand request,
        CancellationToken cancellationToken)
    {
        var type = await _types.GetByIdAsync(request.DecisionTypeId, cancellationToken);
        if (type is null)
            return Result.Failure<DecisionTypeResponse>(DomainErrors.Decisions.TypeNotFound);

        if (type.DestinationDepartmentId is { } destinationId)
        {
            var department = await _departments.GetByIdAsync(destinationId, cancellationToken);
            if (department is null)
                return Result.Failure<DecisionTypeResponse>(DomainErrors.Department.NotFound);
            if (!department.IsActive)
                return Result.Failure<DecisionTypeResponse>(DomainErrors.Department.Inactive);
        }

        var activated = type.Activate(_unitOfWork.UtcNow);
        if (activated.IsFailure)
            return Result.Failure<DecisionTypeResponse>(activated.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(type));
    }
}

internal sealed class DeactivateDecisionTypeCommandHandler
    : ICommandHandler<DeactivateDecisionTypeCommand, DecisionTypeResponse>
{
    private readonly IDecisionTypeRepository _types;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateDecisionTypeCommandHandler(IDecisionTypeRepository types, IUnitOfWork unitOfWork)
    {
        _types = types;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DecisionTypeResponse>> Handle(
        DeactivateDecisionTypeCommand request,
        CancellationToken cancellationToken)
    {
        var type = await _types.GetByIdAsync(request.DecisionTypeId, cancellationToken);
        if (type is null)
            return Result.Failure<DecisionTypeResponse>(DomainErrors.Decisions.TypeNotFound);

        type.Deactivate(_unitOfWork.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DecisionMapping.ToResponse(type));
    }
}

internal sealed class ListDecisionTypesQueryHandler
    : IQueryHandler<ListDecisionTypesQuery, IReadOnlyList<DecisionTypeResponse>>
{
    private readonly IDecisionTypeRepository _types;

    public ListDecisionTypesQueryHandler(IDecisionTypeRepository types) => _types = types;

    public async Task<Result<IReadOnlyList<DecisionTypeResponse>>> Handle(
        ListDecisionTypesQuery request,
        CancellationToken cancellationToken)
    {
        var types = await _types.ListAsync(request.DocumentTypeId, request.IsActive, cancellationToken);
        IReadOnlyList<DecisionTypeResponse> responses = types.Select(DecisionMapping.ToResponse).ToList();
        return Result.Success(responses);
    }
}

internal static class DecisionTypeChecks
{
    public static async Task<Result<Guid?>> DestinationAsync(
        IDepartmentRepository departments,
        int effectId,
        Guid? destinationDepartmentId,
        bool requireActive,
        CancellationToken cancellationToken)
    {
        if (destinationDepartmentId is null || destinationDepartmentId == Guid.Empty || effectId != DecisionEffect.ReferId)
            return Result.Success<Guid?>(null);

        var department = await departments.GetByIdAsync(destinationDepartmentId.Value, cancellationToken);
        if (department is null)
            return Result.Failure<Guid?>(DomainErrors.Department.NotFound);

        if (requireActive && !department.IsActive)
            return Result.Failure<Guid?>(DomainErrors.Department.Inactive);

        return Result.Success<Guid?>(department.DepartmentId);
    }

    public static async Task<Result<IReadOnlyList<int>>> DocumentTypesAsync(
        IDocumentTypeRepository documentTypes,
        IReadOnlyList<int> documentTypeIds,
        CancellationToken cancellationToken)
    {
        var ids = documentTypeIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
            return Result.Failure<IReadOnlyList<int>>(DomainErrors.Decisions.TypeNotAllowedForDocument);

        foreach (var id in ids)
        {
            if (await documentTypes.GetByIdAsync(id, cancellationToken) is null)
                return Result.Failure<IReadOnlyList<int>>(DomainErrors.Correspondence.DocumentTypeNotFound);
        }

        return Result.Success<IReadOnlyList<int>>(ids);
    }
}
