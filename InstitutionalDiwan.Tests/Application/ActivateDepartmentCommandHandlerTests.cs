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
}
