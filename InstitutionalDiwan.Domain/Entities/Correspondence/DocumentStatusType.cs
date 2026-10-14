using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class DocumentStatusType : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public bool IsTerminal { get; private set; }
    public bool IsSystemStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentStatusType()
    {
    }

    public static Result<DocumentStatusType> Create(
        string? code,
        string? nameAr,
        bool isTerminal = false,
        bool isSystemStatus = true,
        string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<DocumentStatusType>(DomainErrors.Correspondence.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<DocumentStatusType>(DomainErrors.Correspondence.TitleEmpty);

        var statusType = new DocumentStatusType
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            IsTerminal = isTerminal,
            IsSystemStatus = isSystemStatus,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(statusType);
    }
}
