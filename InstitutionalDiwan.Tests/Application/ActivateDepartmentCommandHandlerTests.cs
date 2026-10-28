using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ActivateDepartmentCommandHandlerTests
{
    private const int TypeId = DepartmentType.SectionId;

    [Fact]
    public async Task Handle_WhenDepartmentIsInactive_Reactivates()
    {
        var departments = new FakeDepartmentRepository();
        var department = Department.Create("HR", "قسم", TypeId);
        Assert.True(department.IsSuccess);
        department.Value.Deactivate(hasActiveChildren: false);
        departments.Add(department.Value);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ActivateDepartmentCommandHandler(departments, unitOfWork).Handle(
            new ActivateDepartmentCommand(department.Value.DepartmentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.True(department.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsMissing_ReturnsNotFound()
    {
        var result = await new ActivateDepartmentCommandHandler(new FakeDepartmentRepository(), new FakeUnitOfWork())
            .Handle(new ActivateDepartmentCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenInactiveChildHasMissingParent_ReturnsValidationWithoutActivating()
    {
        var departments = new FakeDepartmentRepository();
        var child = Department.Create("CHILD", "قسم", TypeId, Guid.NewGuid()).Value;
        child.Deactivate(hasActiveChildren: false);
        child.ClearDomainEvents();
        var updatedAt = child.UpdatedAt;
        departments.Add(child);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ActivateDepartmentCommandHandler(departments, unitOfWork).Handle(
            new ActivateDepartmentCommand(child.DepartmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.ParentNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.False(child.IsActive);
        Assert.Equal(updatedAt, child.UpdatedAt);
        Assert.Empty(child.DomainEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenParentIsInactive_RequiresParentActivationFirst()
    {
        var departments = new FakeDepartmentRepository();
        var parent = Department.Create("PARENT", "مديرية", DepartmentType.DirectorateId).Value;
        var child = Department.Create("CHILD", "قسم", TypeId, parent.DepartmentId).Value;
        child.Deactivate(hasActiveChildren: false);
        parent.Deactivate(hasActiveChildren: false);
        child.ClearDomainEvents();
        var updatedAt = child.UpdatedAt;
        departments.Add(parent);
        departments.Add(child);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ActivateDepartmentCommandHandler(departments, unitOfWork);

        var blocked = await handler.Handle(new ActivateDepartmentCommand(child.DepartmentId), CancellationToken.None);

        Assert.True(blocked.IsFailure);
        Assert.Equal(DomainErrors.Department.InactiveParent.Code, blocked.Error.Code);
        Assert.Equal(ErrorType.Conflict, blocked.Error.Type);
        Assert.False(child.IsActive);
        Assert.Equal(updatedAt, child.UpdatedAt);
        Assert.Empty(child.DomainEvents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);

        var parentResult = await handler.Handle(new ActivateDepartmentCommand(parent.DepartmentId), CancellationToken.None);
        var childResult = await handler.Handle(new ActivateDepartmentCommand(child.DepartmentId), CancellationToken.None);

        Assert.True(parentResult.IsSuccess);
        Assert.True(childResult.IsSuccess);
        Assert.True(parent.IsActive);
        Assert.True(child.IsActive);
        Assert.Equal(2, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenChildIsAlreadyActive_DoesNotRevalidateParent()
    {
        var departments = new FakeDepartmentRepository();
        var child = Department.Create("CHILD", "قسم", TypeId, Guid.NewGuid()).Value;
        departments.Add(child);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new ActivateDepartmentCommandHandler(departments, unitOfWork).Handle(
            new ActivateDepartmentCommand(child.DepartmentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }
}
