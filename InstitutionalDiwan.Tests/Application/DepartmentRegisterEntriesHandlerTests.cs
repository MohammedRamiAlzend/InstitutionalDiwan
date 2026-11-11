using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// The rules around a department's register page: who may read it, which record form versions it
/// accepts, and how a sort on a form field is handed to the query. The query itself runs in the
/// SQLite tests.
/// </summary>
public sealed class DepartmentRegisterEntriesHandlerTests
{
    private static readonly Guid Admin = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private sealed class World
    {
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeDocumentTypeSchemaVersionRepository Forms { get; } = new();
        public FakeDepartmentScopeEvaluator Scope { get; } = new() { InstitutionWide = true };
        public Department Hr { get; }
        public Department Other { get; }

        public World()
        {
            Hr = Department.Create("HR", "الموارد", 1).Value;
            Other = Department.Create("OTH", "قسم آخر", 1).Value;
            Departments.Add(Hr);
            Departments.Add(Other);
        }

        public ListDepartmentRegisterEntriesQueryHandler Entries() =>
            new(Departments, Numbers, Forms, Scope, new FakeUserContext(), TestClock.Instance);

        public GetDepartmentRegisterFormsQueryHandler FormsHandler() =>
            new(Departments, Forms, new FakeDocumentFieldOptionResolver(), Scope);

        public DocumentTypeSchemaVersion AddForm(Department department, int version, bool publish = true)
        {
            var field = DocumentField.Create(
                DocumentType.InternalMemoId, "case_no", DocumentFieldDataType.Text, Admin,
                departmentId: department.DepartmentId).Value;
            var form = DocumentTypeSchemaVersion.CreateDraft(
                DocumentType.InternalMemoId, version, Admin, departmentId: department.DepartmentId).Value;
            Assert.True(form.AddField(field, DocumentFieldSettings.Create(DocumentFieldDataType.Text, "رقم القضية", false, 1).Value).IsSuccess);
            if (publish)
                Assert.True(form.Publish(Admin).IsSuccess);
            Forms.Add(form);
            return form;
        }
    }

    [Fact]
    public async Task ADepartmentOutsideTheCallersScope_IsForbidden()
    {
        var world = new World();
        world.Scope.InstitutionWide = false;

        var result = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(world.Hr.DepartmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DepartmentOutOfScope.Code, result.Error.Code);
        Assert.Null(world.Numbers.LastRegisterFilter);
    }

    [Fact]
    public async Task AnUnknownDepartment_IsNotFound()
    {
        var world = new World();

        var result = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(DomainErrors.Department.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task AFormOfAnotherDepartmentOrADraft_IsNotAcceptable()
    {
        var world = new World();
        var foreign = world.AddForm(world.Other, 1);
        var draft = world.AddForm(world.Hr, 1, publish: false);

        foreach (var versionId in new[] { foreign.SchemaVersionId, draft.SchemaVersionId, Guid.NewGuid() })
        {
            var result = await world.Entries().Handle(
                new ListDepartmentRegisterEntriesQuery(world.Hr.DepartmentId, SchemaVersionId: versionId),
                CancellationToken.None);

            Assert.True(result.IsFailure);
            Assert.Equal(DomainErrors.DepartmentRecords.FormNotFound.Code, result.Error.Code);
        }
    }

    [Fact]
    public async Task AFormVersionOfTheDepartment_NarrowsByItsTypeAndVersion()
    {
        var world = new World();
        var form = world.AddForm(world.Hr, 2);

        var result = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(
                world.Hr.DepartmentId, SchemaVersionId: form.SchemaVersionId, SortBy: "field:case_no", SortDescending: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var filter = world.Numbers.LastRegisterFilter!;
        Assert.Equal(form.SchemaVersionId, filter.SchemaVersionId);
        Assert.Equal(DocumentType.InternalMemoId, filter.DocumentTypeId);
        Assert.Equal(form.Fields.Single().DocumentFieldId, filter.FieldSort!.DocumentFieldId);
    }

    [Fact]
    public async Task ASortOnAFieldTheFormLacks_FallsBackToTheDefaultOrder()
    {
        var world = new World();
        var form = world.AddForm(world.Hr, 1);

        var result = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(
                world.Hr.DepartmentId, SchemaVersionId: form.SchemaVersionId, SortBy: "field:missing"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Null(world.Numbers.LastRegisterFilter!.FieldSort);
    }

    [Fact]
    public async Task FieldFilters_NeedAFormVersion()
    {
        var world = new World();

        var result = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(world.Hr.DepartmentId, FieldFilters: new[] { "case_no:contains:7" }),
            CancellationToken.None);

        Assert.Equal(DomainErrors.DocumentFields.FilterRequiresDocumentType.Code, result.Error.Code);
    }

    [Fact]
    public async Task FieldFilters_AreCheckedAgainstTheFormsFields_ThenHandedToTheQuery()
    {
        var world = new World();
        var form = world.AddForm(world.Hr, 1);

        var unknown = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(
                world.Hr.DepartmentId, SchemaVersionId: form.SchemaVersionId, FieldFilters: new[] { "missing:eq:1" }),
            CancellationToken.None);
        Assert.Equal(DomainErrors.DocumentFields.FilterFieldNotFound.Code, unknown.Error.Code);

        var good = await world.Entries().Handle(
            new ListDepartmentRegisterEntriesQuery(
                world.Hr.DepartmentId, SchemaVersionId: form.SchemaVersionId, FieldFilters: new[] { "case_no:contains:7" }),
            CancellationToken.None);
        Assert.True(good.IsSuccess, good.Error?.Code);
        var filter = Assert.Single(world.Numbers.LastRegisterFilter!.FieldFilters!);
        Assert.Equal(form.Fields.Single().DocumentFieldId, filter.DocumentFieldId);
        Assert.Equal(FieldFilterOperator.Contains, filter.Operator);
        Assert.Equal("7", filter.Text);
    }

    [Fact]
    public async Task TheFormsList_IsPerTypeNewestFirst_WithoutDraftsOrOtherDepartments()
    {
        var world = new World();
        world.AddForm(world.Hr, 1);
        var latest = world.AddForm(world.Hr, 2);
        world.AddForm(world.Hr, 3, publish: false);
        world.AddForm(world.Other, 1);

        var result = await world.FormsHandler().Handle(
            new GetDepartmentRegisterFormsQuery(world.Hr.DepartmentId), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var form = Assert.Single(result.Value.Forms);
        Assert.Equal(DocumentType.InternalMemoId, form.DocumentTypeId);
        Assert.Equal(new[] { 2, 1 }, form.Versions.Select(version => version.VersionNumber).ToArray());
        Assert.Equal(latest.SchemaVersionId, form.Versions[0].SchemaVersionId);
    }

    [Fact]
    public async Task TheFormsList_OfADepartmentOutsideTheScope_IsForbidden()
    {
        var world = new World();
        world.Scope.InstitutionWide = false;

        var result = await world.FormsHandler().Handle(
            new GetDepartmentRegisterFormsQuery(world.Hr.DepartmentId), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.DepartmentOutOfScope.Code, result.Error.Code);
    }
}
