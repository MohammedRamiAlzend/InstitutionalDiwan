using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class DocumentType : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string AllowedDirectionScope { get; private set; } = "ALL";
    public bool RequiresOfficialNumber { get; private set; } = true;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private DocumentType()
    {
    }

    public static Result<DocumentType> Create(
        string? code,
        string? nameAr,
        string allowedDirectionScope = "ALL",
        bool requiresOfficialNumber = true,
        string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<DocumentType>(DomainErrors.Correspondence.TypeCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<DocumentType>(DomainErrors.Correspondence.TitleEmpty);

        var docType = new DocumentType
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            AllowedDirectionScope = string.IsNullOrWhiteSpace(allowedDirectionScope) ? "ALL" : allowedDirectionScope.Trim().ToUpperInvariant(),
            RequiresOfficialNumber = requiresOfficialNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(docType);
    }
}
