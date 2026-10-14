using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Relations;

public sealed class DocumentRelationType : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public bool IsSymmetric { get; private set; }
    public bool PreventCycles { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentRelationType()
    {
    }

    public static Result<DocumentRelationType> Create(
        string? code,
        string? nameAr,
        bool isSymmetric = false,
        bool preventCycles = true,
        string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<DocumentRelationType>(DomainErrors.Relations.RelationTypeEmpty);

        var relationType = new DocumentRelationType
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            IsSymmetric = isSymmetric,
            PreventCycles = preventCycles,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(relationType);
    }

    public (Guid SourceId, Guid TargetId) NormalizeEndpoints(Guid sourceDocumentId, Guid targetDocumentId)
    {
        if (!IsSymmetric)
            return (sourceDocumentId, targetDocumentId);

        return sourceDocumentId.CompareTo(targetDocumentId) <= 0
            ? (sourceDocumentId, targetDocumentId)
            : (targetDocumentId, sourceDocumentId);
    }
}
