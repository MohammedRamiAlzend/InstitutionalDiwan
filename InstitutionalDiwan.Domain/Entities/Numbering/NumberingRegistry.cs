using System;
using System.Collections.Generic;
using System.Linq;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Numbering;

public sealed class NumberingRegistry : AggregateRoot<Guid>
{
    private readonly List<NumberingCounter> _counters = new();

    public Guid RegistryId
    {
        get => Id;
        private set => Id = value;
    }

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid? DepartmentId { get; private set; }
    // FK → DocumentType.Code (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentType)
    public string? DocumentTypeCode { get; private set; }
    public string? Direction { get; private set; }
    public string FormatPattern { get; private set; } = "{YEAR}/{SEQ:D6}";
    public string ResetPolicy { get; private set; } = "YEARLY";
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<NumberingCounter> Counters => _counters.AsReadOnly();

    private NumberingRegistry()
    {
    }

    public static Result<NumberingRegistry> Create(
        string? code,
        string? nameAr,
        string formatPattern = "{YEAR}/{SEQ:D6}",
        string resetPolicy = "YEARLY",
        Guid? departmentId = null,
        string? documentTypeCode = null,
        string? direction = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<NumberingRegistry>(DomainErrors.Numbering.RegistryCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<NumberingRegistry>(DomainErrors.Numbering.RegistryCodeEmpty);

        var registry = new NumberingRegistry
        {
            RegistryId = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            DepartmentId = departmentId,
            DocumentTypeCode = string.IsNullOrWhiteSpace(documentTypeCode) ? null : documentTypeCode.Trim().ToUpperInvariant(),
            Direction = string.IsNullOrWhiteSpace(direction) ? null : direction.Trim().ToUpperInvariant(),
            FormatPattern = string.IsNullOrWhiteSpace(formatPattern) ? "{YEAR}/{SEQ:D6}" : formatPattern.Trim(),
            ResetPolicy = string.IsNullOrWhiteSpace(resetPolicy) ? "YEARLY" : resetPolicy.Trim().ToUpperInvariant(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(registry);
    }

    public Result<long> AllocateNextSequence(int numberingYear)
    {
        if (!IsActive)
            return Result.Failure<long>(DomainErrors.Numbering.RegistryInactive);

        if (numberingYear is < 1900 or > 2200)
            return Result.Failure<long>(DomainErrors.Numbering.YearInvalid);

        var counter = _counters.FirstOrDefault(c => c.NumberingYear == numberingYear);
        if (counter is null)
        {
            counter = NumberingCounter.Create(RegistryId, numberingYear);
            _counters.Add(counter);
        }

        return Result.Success(counter.Increment());
    }
}
