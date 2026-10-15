using System;
using System.Collections.Generic;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Organization;

public sealed class Department : AggregateRoot<Guid>
{
    public Guid DepartmentId
    {
        get => Id;
        private set => Id = value;
    }

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string DepartmentTypeCode { get; private set; } = null!;
    public Guid? ParentDepartmentId { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public bool IsActive { get; private set; }
    public bool CanSendExternal { get; private set; }
    public bool CanReceiveExternal { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Department()
    {
    }

    private Department(
        Guid departmentId,
        DepartmentCode code,
        string nameAr,
        string? nameEn,
        string departmentTypeCode,
        Guid? parentDepartmentId,
        bool canSendExternal,
        bool canReceiveExternal,
        string? notes)
        : base(departmentId)
    {
        DepartmentId = departmentId;
        Code = code.Value;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        DepartmentTypeCode = departmentTypeCode.Trim().ToUpperInvariant();
        ParentDepartmentId = parentDepartmentId;
        IsActive = true;
        CanSendExternal = canSendExternal;
        CanReceiveExternal = canReceiveExternal;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DepartmentCreatedEvent(DepartmentId, Code, NameAr, ParentDepartmentId));
    }

    public static Result<Department> Create(
        string? code,
        string? nameAr,
        string? departmentTypeCode,
        Guid? parentDepartmentId = null,
        string? nameEn = null,
        bool canSendExternal = false,
        bool canReceiveExternal = false,
        string? notes = null)
    {
        var codeResult = DepartmentCode.Create(code);
        if (codeResult.IsFailure)
            return Result.Failure<Department>(codeResult.Error);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<Department>(DomainErrors.Department.NameEmpty);

        if (string.IsNullOrWhiteSpace(departmentTypeCode))
            return Result.Failure<Department>(DomainErrors.Department.TypeEmpty);

        var department = new Department(
            Guid.NewGuid(),
            codeResult.Value,
            nameAr,
            nameEn,
            departmentTypeCode,
            parentDepartmentId,
            canSendExternal,
            canReceiveExternal,
            notes);

        return Result.Success(department);
    }

    public Result Rename(string? nameAr, string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure(DomainErrors.Department.NameEmpty);

        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result MoveToParent(Guid? newParentId, IReadOnlySet<Guid>? descendantDepartmentIds = null, bool newParentIsActive = true)
    {
        if (newParentId.HasValue)
        {
            if (newParentId.Value == DepartmentId)
                return Result.Failure(DomainErrors.Department.SelfParent);

            if (!newParentIsActive)
                return Result.Failure(DomainErrors.Department.InactiveParent);

            if (descendantDepartmentIds is not null && descendantDepartmentIds.Contains(newParentId.Value))
                return Result.Failure(DomainErrors.Department.CyclicHierarchy);
        }

        var oldParentId = ParentDepartmentId;
        ParentDepartmentId = newParentId;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new DepartmentMovedEvent(DepartmentId, oldParentId, newParentId));
        return Result.Success();
    }

    public Result AssignManager(Guid? managerEmployeeId)
    {
        ManagerEmployeeId = managerEmployeeId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateExternalCorrespondencePolicy(bool canSendExternal, bool canReceiveExternal)
    {
        CanSendExternal = canSendExternal;
        CanReceiveExternal = canReceiveExternal;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate(bool hasActiveChildren)
    {
        if (hasActiveChildren)
            return Result.Failure(DomainErrors.Department.HasActiveChildren);

        if (!IsActive)
            return Result.Success();

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new DepartmentDeactivatedEvent(DepartmentId));
        return Result.Success();
    }

    public Result Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
