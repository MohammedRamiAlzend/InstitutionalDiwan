namespace InstitutionalDiwan.Domain.Repositories;

/// <summary>The department visit a decision is issued against: unlocked, and owned by a department.</summary>
public sealed record OpenDecisionVisit(Guid VisitId, Guid DepartmentId);

/// <summary>Filters for the decision register. Null members are not applied.</summary>
public sealed record DecisionRegisterFilter(
    Guid? DecisionTypeId = null,
    int? StatusId = null,
    Guid? IssuingDepartmentId = null,
    int? Year = null);

public interface IDecisionTypeRepository
{
    Task<DecisionType?> GetByIdAsync(Guid decisionTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DecisionType>> ListAsync(
        int? documentTypeId,
        bool? isActive,
        CancellationToken cancellationToken = default);
    void Add(DecisionType decisionType);
}

public interface IDocumentDecisionRepository
{
    Task<DocumentDecision?> GetByIdAsync(Guid decisionId, CancellationToken cancellationToken = default);
    Task<DocumentDecision?> GetActiveByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentDecision>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<bool> IsNumberTakenAsync(
        Guid issuingDepartmentId,
        int year,
        string decisionNumber,
        CancellationToken cancellationToken = default);
    Task<OpenDecisionVisit?> FindOpenVisitAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<PagedList<DecisionRegisterRow>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        DecisionRegisterFilter filter,
        CancellationToken cancellationToken = default);
    void Add(DocumentDecision decision);
}

/// <summary>One row of the decision register, joined to the document the caller is allowed to see.</summary>
public sealed record DecisionRegisterRow(
    DocumentDecision Decision,
    string TypeNameAr,
    string DocumentTitle,
    string? OfficialNumberText);
