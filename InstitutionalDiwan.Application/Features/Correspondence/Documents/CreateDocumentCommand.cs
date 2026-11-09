using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Creates a new document owned by the caller's department and registers it in one step.
/// <c>UserId</c> is derived from the token by the endpoint, never from the request body.
/// <list type="bullet">
/// <item>The central ("system") number is issued by the system as a GUID; the numbering year is the
/// current year. The caller never supplies either.</item>
/// <item><c>DepartmentRegistryNumberText</c> and <c>DepartmentRegistryDate</c> are the number and date
/// the user writes in the caller's own department register; the department must have an active
/// register or creation fails.</item>
/// <item><c>Movement</c> is optional: when present the document is routed to the destination
/// department and the movement is sent immediately.</item>
/// </list>
/// Everything runs in one transaction. <c>DirectionId</c>, <c>ConfidentialityLevelId</c> and
/// <c>PriorityLevelId</c> are suspended: all three are optional and stay null unless a caller sends them.
/// </summary>
public sealed record CreateDocumentCommand(
    Guid UserId,
    string Title,
    int DocumentTypeId,
    string DepartmentRegistryNumberText,
    DateTime DepartmentRegistryDate,
    string? SubjectSummary = null,
    int? DirectionId = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null,
    IReadOnlyList<DocumentFieldValueInput>? FieldValues = null,
    CreateDocumentMovementInput? Movement = null,
    Guid? DepartmentRegistryId = null,
    IReadOnlyList<DocumentPersonInput>? Persons = null) : ICommand<DocumentResponse>;

/// <summary>Where the new document is sent as soon as it is created.</summary>
public sealed record CreateDocumentMovementInput(
    Guid DestinationDepartmentId,
    int MovementTypeId = MovementType.ReferralId,
    Guid? AssignedToUserId = null,
    string? Instructions = null,
    DateTime? DueDate = null);
