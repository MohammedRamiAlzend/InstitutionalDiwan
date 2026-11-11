using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>The department register page (search, filters, sort, paging and record values) against SQLite.</summary>
public sealed class DepartmentRegisterPageTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        DiwanDbContext Db,
        Department Department,
        NumberingRegistry BookA,
        NumberingRegistry BookB,
        User Actor,
        DocumentTypeSchemaVersion FormV1,
        DocumentTypeSchemaVersion FormV2);

    private static async Task<World> CreateWorldAsync()
    {
        var db = TestContextFactory.Create();

        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        db.UserScopeTypes.Add(UserScopeType.Create(UserScopeType.DepartmentId, "Department", "قسم").Value);
        db.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        db.DocumentTypes.Add(DocumentType.Create(DocumentType.InternalMemoId, "Memo", "مذكرة", DocumentDirection.InternalId).Value);
        db.DocumentStatusTypes.Add(DocumentStatusType.Create(DocumentStatusType.DraftId, "Draft", "مسودة").Value);
        db.DocumentNumberScopes.AddRange(
            DocumentNumberScope.Create(DocumentNumberScope.CentralId, "Central", "مركزي").Value,
            DocumentNumberScope.Create(DocumentNumberScope.DepartmentId, "Department", "قسم").Value);
        db.DocumentNumberStatuses.Add(DocumentNumberStatus.Create(DocumentNumberStatus.ActiveId, "Active", "فعال").Value);
        db.NumberingResetPolicies.Add(NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);
        foreach (var id in new[]
                 {
                     DocumentNumberEventType.IssuedId,
                     DocumentNumberEventType.RegisteredInDepartmentId,
                     DocumentNumberEventType.ReRegisteredInDepartmentId
                 })
            db.DocumentNumberEventTypes.Add(DocumentNumberEventType.Create(id, $"Event {id}", $"حدث {id}").Value);

        var department = Department.Create("HR", "الموارد", DepartmentType.SectionId).Value;
        db.Departments.Add(department);
        var actor = User.Create("actor", "hash", department.DepartmentId).Value;
        db.Users.Add(actor);

        var bookA = NumberingRegistry.Create("BOOK-A", "السجل أ", departmentId: department.DepartmentId).Value;
        var bookB = NumberingRegistry.Create("BOOK-B", "السجل ب", departmentId: department.DepartmentId).Value;
        db.NumberingRegistries.AddRange(bookA, bookB);

        var field = DocumentField.Create(
            DocumentType.InternalMemoId, "case_no", DocumentFieldDataType.Text, actor.UserId,
            departmentId: department.DepartmentId).Value;
        db.DocumentFields.Add(field);
        var formV1 = PublishForm(db, department, actor, field, version: 1);
        var formV2 = PublishForm(db, department, actor, field, version: 2);

        await db.SaveChangesAsync();
        return new World(db, department, bookA, bookB, actor, formV1, formV2);
    }

    /// <summary>A published record form version holding the shared text field.</summary>
    private static DocumentTypeSchemaVersion PublishForm(
        DiwanDbContext db, Department department, User actor, DocumentField field, int version)
    {
        var form = DocumentTypeSchemaVersion.CreateDraft(
            DocumentType.InternalMemoId, version, actor.UserId, departmentId: department.DepartmentId).Value;
        Assert.True(form.AddField(field, DocumentFieldSettings.Create(DocumentFieldDataType.Text, "رقم القضية", false, 1).Value).IsSuccess);
        Assert.True(form.Publish(actor.UserId).IsSuccess);
        db.DocumentTypeSchemaVersions.Add(form);
        return form;
    }

    private static Document AddDocument(World world, string name)
    {
        var document = Document.CreateDraft(
            null, DocumentType.InternalMemoId, DocumentDirection.InternalId, world.Department.DepartmentId,
            world.Actor.UserId, utcNow: Now, displayName: name).Value;
        world.Db.Documents.Add(document);
        return document;
    }

    private static DocumentNumber AddLine(
        World world, Document document, NumberingRegistry book, string number, DateTime date,
        DocumentTypeSchemaVersion? form = null, string? caseNo = null)
    {
        var line = DocumentNumber.CreateDepartmentEntry(
            document.DocumentId, world.Department.DepartmentId, book.RegistryId, number, date,
            world.Actor.UserId, utcNow: Now).Value;
        Assert.True(document.AddDepartmentNumber(line).IsSuccess);

        if (form is not null)
        {
            var inputs = caseNo is null
                ? Array.Empty<DocumentFieldValueInput>()
                : new[] { new DocumentFieldValueInput("case_no", caseNo) };
            var opened = line.Visits.Single().OpenRecord(form, inputs, world.Actor.UserId, Now, requireComplete: false);
            Assert.True(opened.IsSuccess, opened.Error?.Code);
        }

        return line;
    }

    private static Task<PagedList<DepartmentRegisterRow>> PageAsync(
        World world,
        DepartmentRegisterFilter? filter = null,
        PageRequest? page = null,
        IReadOnlyCollection<Guid>? scope = null) =>
        new DocumentNumberRepository(world.Db).ListDepartmentRegisterPagedAsync(
            page ?? new PageRequest(1, 50, null, "registryNumber", false),
            new DocumentViewAccess(world.Actor.UserId, scope, Now),
            filter ?? new DepartmentRegisterFilter(world.Department.DepartmentId));

    private static string[] Numbers(PagedList<DepartmentRegisterRow> result) =>
        result.Items.Select(row => row.Number.OfficialNumberText).ToArray();

    [Fact]
    public async Task NarrowsByRegisterYearAndDayRange_AndCountsBeforePaging()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "أ"), world.BookA, "1/2026", new DateTime(2026, 3, 1));
        AddLine(world, AddDocument(world, "ب"), world.BookA, "2/2026", new DateTime(2026, 3, 9));
        AddLine(world, AddDocument(world, "ج"), world.BookB, "1/2026", new DateTime(2026, 3, 9));
        AddLine(world, AddDocument(world, "د"), world.BookA, "1/2025", new DateTime(2025, 12, 31));
        await world.Db.SaveChangesAsync();
        var departmentId = world.Department.DepartmentId;

        var inBookA = await PageAsync(world, new DepartmentRegisterFilter(departmentId, RegistryId: world.BookA.RegistryId));
        Assert.Equal(new[] { "1/2025", "1/2026", "2/2026" }, Numbers(inBookA));

        var year2026 = await PageAsync(world, new DepartmentRegisterFilter(departmentId, world.BookA.RegistryId, NumberingYear: 2026));
        Assert.Equal(new[] { "1/2026", "2/2026" }, Numbers(year2026));

        var onTheNinth = await PageAsync(world, new DepartmentRegisterFilter(
            departmentId, RegistryDateFrom: new DateTime(2026, 3, 9), RegistryDateTo: new DateTime(2026, 3, 9)));
        Assert.Equal(new[] { "1/2026", "2/2026" }, Numbers(onTheNinth));

        var secondPage = await PageAsync(
            world,
            new DepartmentRegisterFilter(departmentId, world.BookA.RegistryId),
            new PageRequest(2, 2, null, "registryNumber", false));
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(new[] { "2/2026" }, Numbers(secondPage));
    }

    [Fact]
    public async Task ARegisterLineOfAnotherDepartmentNeverAppears()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "أ"), world.BookA, "1/2026", new DateTime(2026, 3, 1));
        await world.Db.SaveChangesAsync();

        var other = await PageAsync(world, new DepartmentRegisterFilter(Guid.NewGuid()));

        Assert.Empty(other.Items);
        Assert.Equal(0, other.TotalCount);
    }

    [Fact]
    public async Task ChoosingAFormVersionKeepsOnlyItsLines_WithTheLatestVisitValues()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "على الإصدار الأول"), world.BookA, "1/2026", new DateTime(2026, 3, 1), world.FormV1, "A-100");
        AddLine(world, AddDocument(world, "على الإصدار الثاني"), world.BookA, "2/2026", new DateTime(2026, 3, 2), world.FormV2, "B-200");
        AddLine(world, AddDocument(world, "بلا نموذج"), world.BookA, "3/2026", new DateTime(2026, 3, 3));
        await world.Db.SaveChangesAsync();
        var departmentId = world.Department.DepartmentId;
        var field = world.FormV2.Fields.Single();

        var v2 = await PageAsync(world, new DepartmentRegisterFilter(
            departmentId, DocumentTypeId: DocumentType.InternalMemoId, SchemaVersionId: world.FormV2.SchemaVersionId));

        var row = Assert.Single(v2.Items);
        Assert.Equal("2/2026", row.Number.OfficialNumberText);
        var value = Assert.Single(row.RecordVisit!.Values);
        Assert.Equal(field.DocumentFieldId, value.DocumentFieldId);
        Assert.Equal("B-200", value.TextValue);

        var everything = await PageAsync(world);
        Assert.Equal(3, everything.TotalCount);
        Assert.All(everything.Items, line => Assert.Null(line.RecordVisit));
    }

    [Fact]
    public async Task SearchFindsTheNumberTheTitleAndTheFormValues()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "عقد توريد"), world.BookA, "10/2026", new DateTime(2026, 3, 1), world.FormV1, "CASE-777");
        AddLine(world, AddDocument(world, "طلب إجازة"), world.BookA, "11/2026", new DateTime(2026, 3, 2), world.FormV1, "CASE-888");
        await world.Db.SaveChangesAsync();
        var departmentId = world.Department.DepartmentId;
        var form = new DepartmentRegisterFilter(departmentId, SchemaVersionId: world.FormV1.SchemaVersionId);

        Assert.Equal(new[] { "10/2026" }, Numbers(await PageAsync(world, form, new PageRequest(1, 50, "عقد"))));
        Assert.Equal(new[] { "11/2026" }, Numbers(await PageAsync(world, form, new PageRequest(1, 50, "11/20"))));
        Assert.Equal(new[] { "11/2026" }, Numbers(await PageAsync(world, form, new PageRequest(1, 50, "CASE-888"))));

        // Without a chosen form the values are not searched.
        Assert.Empty((await PageAsync(world, new DepartmentRegisterFilter(departmentId), new PageRequest(1, 50, "CASE-888"))).Items);
    }

    [Fact]
    public async Task SortsByAFormField()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "أ"), world.BookA, "1/2026", new DateTime(2026, 3, 1), world.FormV1, "C-3");
        AddLine(world, AddDocument(world, "ب"), world.BookA, "2/2026", new DateTime(2026, 3, 2), world.FormV1, "A-1");
        AddLine(world, AddDocument(world, "ج"), world.BookA, "3/2026", new DateTime(2026, 3, 3), world.FormV1, "B-2");
        await world.Db.SaveChangesAsync();
        var field = world.FormV1.Fields.Single();
        var filter = new DepartmentRegisterFilter(
            world.Department.DepartmentId,
            SchemaVersionId: world.FormV1.SchemaVersionId,
            FieldSort: new DocumentFieldSort(field.DocumentFieldId, DocumentFieldDataType.Text));

        var ascending = await PageAsync(world, filter, new PageRequest(1, 50, null, null, false));
        var descending = await PageAsync(world, filter, new PageRequest(1, 50, null, null, true));

        Assert.Equal(new[] { "2/2026", "3/2026", "1/2026" }, Numbers(ascending));
        Assert.Equal(new[] { "1/2026", "3/2026", "2/2026" }, Numbers(descending));
    }

    [Fact]
    public async Task FiltersByAFormFieldValue_OnTheChosenVersionOnly()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "أ"), world.BookA, "1/2026", new DateTime(2026, 3, 1), world.FormV1, "CASE-777");
        AddLine(world, AddDocument(world, "ب"), world.BookA, "2/2026", new DateTime(2026, 3, 2), world.FormV1, "CASE-888");
        AddLine(world, AddDocument(world, "ج"), world.BookA, "3/2026", new DateTime(2026, 3, 3), world.FormV2, "CASE-777");
        AddLine(world, AddDocument(world, "د"), world.BookA, "4/2026", new DateTime(2026, 3, 4), world.FormV1);
        await world.Db.SaveChangesAsync();
        var fieldId = world.FormV1.Fields.Single().DocumentFieldId;

        DepartmentRegisterFilter On(DocumentTypeSchemaVersion form, DocumentFieldFilter filter) =>
            new(world.Department.DepartmentId, SchemaVersionId: form.SchemaVersionId, FieldFilters: new[] { filter });

        var contains = await PageAsync(world, On(world.FormV1, new DocumentFieldFilter(
            fieldId, DocumentFieldDataType.Text, FieldFilterOperator.Contains, Text: "777")));
        Assert.Equal(new[] { "1/2026" }, Numbers(contains));

        var equals = await PageAsync(world, On(world.FormV1, new DocumentFieldFilter(
            fieldId, DocumentFieldDataType.Text, FieldFilterOperator.Equals, Text: "CASE-888")));
        Assert.Equal(new[] { "2/2026" }, Numbers(equals));
        Assert.Equal(1, equals.TotalCount);

        var otherVersion = await PageAsync(world, On(world.FormV2, new DocumentFieldFilter(
            fieldId, DocumentFieldDataType.Text, FieldFilterOperator.Contains, Text: "777")));
        Assert.Equal(new[] { "3/2026" }, Numbers(otherVersion));

        var nothing = await PageAsync(world, On(world.FormV1, new DocumentFieldFilter(
            fieldId, DocumentFieldDataType.Text, FieldFilterOperator.Contains, Text: "999")));
        Assert.Empty(nothing.Items);
    }

    [Fact]
    public async Task FilterSearchAndSortReadOnlyTheLatestVisit()
    {
        var world = await CreateWorldAsync();
        // Line 1: an old visit said "OLD-1", the latest says "Z-NEW". Line 2 has the single value "M-MID".
        var first = AddLine(world, AddDocument(world, "أ"), world.BookA, "1/2026", new DateTime(2026, 3, 1), world.FormV1, "OLD-1");
        var again = first.RecordVisit(world.Actor.UserId, new DateTime(2026, 4, 1), utcNow: Now.AddMinutes(1));
        Assert.True(again.IsSuccess, again.Error?.Code);
        var reopened = again.Value.OpenRecord(
            world.FormV1, new[] { new DocumentFieldValueInput("case_no", "Z-NEW") }, world.Actor.UserId, Now, requireComplete: false);
        Assert.True(reopened.IsSuccess, reopened.Error?.Code);
        AddLine(world, AddDocument(world, "ب"), world.BookA, "2/2026", new DateTime(2026, 3, 2), world.FormV1, "M-MID");
        await world.Db.SaveChangesAsync();
        var fieldId = world.FormV1.Fields.Single().DocumentFieldId;
        var version = world.FormV1.SchemaVersionId;
        var departmentId = world.Department.DepartmentId;

        DepartmentRegisterFilter Filtered(string text) => new(
            departmentId, SchemaVersionId: version,
            FieldFilters: new[] { new DocumentFieldFilter(fieldId, DocumentFieldDataType.Text, FieldFilterOperator.Contains, Text: text) });

        // The old value no longer matches; the latest one does.
        Assert.Empty((await PageAsync(world, Filtered("OLD"))).Items);
        Assert.Equal(new[] { "1/2026" }, Numbers(await PageAsync(world, Filtered("Z-NEW"))));

        var form = new DepartmentRegisterFilter(departmentId, SchemaVersionId: version);
        Assert.Empty((await PageAsync(world, form, new PageRequest(1, 50, "OLD"))).Items);
        Assert.Equal(new[] { "1/2026" }, Numbers(await PageAsync(world, form, new PageRequest(1, 50, "Z-NEW"))));

        var sorted = form with { FieldSort = new DocumentFieldSort(fieldId, DocumentFieldDataType.Text) };
        Assert.Equal(new[] { "2/2026", "1/2026" }, Numbers(await PageAsync(world, sorted, new PageRequest(1, 50, null, null, false))));
    }

    [Fact]
    public async Task ALineTheCallerCannotViewKeepsItsNumberButNotItsTitle()
    {
        var world = await CreateWorldAsync();
        AddLine(world, AddDocument(world, "كتاب سري"), world.BookA, "9/2026", new DateTime(2026, 3, 1));
        await world.Db.SaveChangesAsync();

        // A stranger outside the owning department: the line is in the book, the document is not theirs to see.
        var stranger = User.Create("stranger", "hash", Guid.NewGuid()).Value;
        world.Db.Users.Add(stranger);
        await world.Db.SaveChangesAsync();
        var access = new DocumentViewAccess(stranger.UserId, new[] { Guid.NewGuid() }, Now);
        var repository = new DocumentNumberRepository(world.Db);
        var filter = new DepartmentRegisterFilter(world.Department.DepartmentId);

        var all = await repository.ListDepartmentRegisterPagedAsync(new PageRequest(1, 50), access, filter);
        var row = Assert.Single(all.Items);
        Assert.Equal("9/2026", row.Number.OfficialNumberText);
        Assert.Null(row.DocumentTitle);

        var byTitle = await repository.ListDepartmentRegisterPagedAsync(new PageRequest(1, 50, "سري"), access, filter);
        Assert.Empty(byTitle.Items);
    }
}
