using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListDepartmentDocumentsQueryHandlerTests
{
    private static Department CreateDepartment(string code)
    {
        var department = Department.Create(code, "قسم", DepartmentType.SectionId);
        Assert.True(department.IsSuccess);
        return department.Value;
    }

    private static Document CreateDocument(string title, Guid owningDepartmentId)
    {
        var document = Document.CreateDraft(
            title,
            DocumentType.OutgoingLetterId,
            DocumentDirection.InternalId,
            owningDepartmentId,
            Guid.NewGuid());
        Assert.True(document.IsSuccess);
        return document.Value;
    }

    private static (FakeDocumentRepository Documents, FakeDepartmentRepository Departments) Arrange()
    {
        var department = CreateDepartment("HR");
        var otherDepartment = CreateDepartment("FIN");

        var departments = new FakeDepartmentRepository();
        departments.Add(department);
        departments.Add(otherDepartment);

        var documents = new FakeDocumentRepository();
        documents.Add(CreateDocument("hr-memo", department.DepartmentId));
        documents.Add(CreateDocument("finance-memo", otherDepartment.DepartmentId));

        return (documents, departments);
    }

    private static ListDepartmentDocumentsQueryHandler CreateHandler(
        FakeDocumentRepository documents,
        FakeDepartmentRepository departments,
        params Guid[] accessibleDepartments)
    {
        var scope = new FakeDepartmentScopeEvaluator();
        foreach (var departmentId in accessibleDepartments)
            scope.AccessibleDepartments.Add(departmentId);

        return new ListDepartmentDocumentsQueryHandler(documents, departments, scope);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyTheDepartmentsDocuments()
    {
        var (documents, departments) = Arrange();
        var departmentId = departments.Departments[0].DepartmentId;

        var result = await CreateHandler(documents, departments, departmentId).Handle(
            new ListDepartmentDocumentsQuery(departmentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var document = Assert.Single(result.Value.Items);
        Assert.Equal("hr-memo", document.Title);
        Assert.Equal(departmentId, document.OwningDepartmentId);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_AppliesTheSearchAndPagingContract()
    {
        var department = CreateDepartment("HR");
        var departments = new FakeDepartmentRepository();
        departments.Add(department);

        var documents = new FakeDocumentRepository();
        for (var i = 0; i < 25; i++)
            documents.Add(CreateDocument($"memo-{i}", department.DepartmentId));

        var result = await CreateHandler(documents, departments, department.DepartmentId).Handle(
            new ListDepartmentDocumentsQuery(department.DepartmentId, Page: 2, PageSize: 10),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Items.Count);
        Assert.Equal(25, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.True(result.Value.HasPrevious);
        Assert.True(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_WhenDepartmentMissing_ReturnsNotFound()
    {
        var (documents, departments) = Arrange();

        var result = await CreateHandler(documents, departments).Handle(
            new ListDepartmentDocumentsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDepartmentIsOutsideTheCallersScope_ReturnsForbidden()
    {
        var (documents, departments) = Arrange();
        var departmentId = departments.Departments[0].DepartmentId;

        var result = await CreateHandler(documents, departments).Handle(
            new ListDepartmentDocumentsQuery(departmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DepartmentOutOfScope.Code, result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }
}
