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

/// <summary>Field-value filters and sorts of the department register page for number, date, boolean and select fields.</summary>
public sealed class DepartmentRegisterFieldTypeFilterTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        DiwanDbContext Db,
        Department Department,
        DocumentTypeSchemaVersion Form,
        IReadOnlyDictionary<string, DocumentField> Fields);

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
                     DocumentNumberEventType.RegisteredInDepartmentId
                 })
            db.DocumentNumberEventTypes.Add(DocumentNumberEventType.Create(id, $"Event {id}", $"حدث {id}").Value);

        var department = Department.Create("HR", "الموارد", DepartmentType.SectionId).Value;
        db.Departments.Add(department);
        var actor = User.Create("actor", "hash", department.DepartmentId).Value;
        db.Users.Add(actor);
        db.NumberingRegistries.Add(NumberingRegistry.Create("BOOK", "السجل", departmentId: department.DepartmentId).Value);

        var form = DocumentTypeSchemaVersion.CreateDraft(
            DocumentType.InternalMemoId, 1, actor.UserId, departmentId: department.DepartmentId).Value;
        var fields = new Dictionary<string, DocumentField>();
        void Add(string key, DocumentFieldDataType type, IReadOnlyList<DocumentFieldOption>? options = null)
        {
            var field = DocumentField.Create(
                DocumentType.InternalMemoId, key, type, actor.UserId, departmentId: department.DepartmentId).Value;
            db.DocumentFields.Add(field);
            fields[key] = field;
            var settings = DocumentFieldSettings.Create(type, key, false, fields.Count, options: options);
            Assert.True(settings.IsSuccess, settings.Error?.Code);
            Assert.True(form.AddField(field, settings.Value).IsSuccess);
        }

        Add("amount", DocumentFieldDataType.Number);
        Add("due", DocumentFieldDataType.Date);
        Add("urgent", DocumentFieldDataType.Boolean);
        Add("kind", DocumentFieldDataType.Select, new[] { new DocumentFieldOption("a", "أ"), new DocumentFieldOption("b", "ب") });
        Assert.True(form.Publish(actor.UserId).IsSuccess);
        db.DocumentTypeSchemaVersions.Add(form);
        await db.SaveChangesAsync();

        var book = db.NumberingRegistries.Single();
        void Line(string number, string amount, string due, string urgent, string kind)
        {
            var document = Document.CreateDraft(
                null, DocumentType.InternalMemoId, DocumentDirection.InternalId, department.DepartmentId,
                actor.UserId, utcNow: Now, displayName: number).Value;
            db.Documents.Add(document);
            var line = DocumentNumber.CreateDepartmentEntry(
                document.DocumentId, department.DepartmentId, book.RegistryId, number, new DateTime(2026, 3, 1),
                actor.UserId, utcNow: Now).Value;
            Assert.True(document.AddDepartmentNumber(line).IsSuccess);
            var opened = line.Visits.Single().OpenRecord(
                form,
                new[]
                {
                    new DocumentFieldValueInput("amount", amount),
                    new DocumentFieldValueInput("due", due),
                    new DocumentFieldValueInput("urgent", urgent),
                    new DocumentFieldValueInput("kind", kind)
                },
                actor.UserId, Now, requireComplete: false);
            Assert.True(opened.IsSuccess, opened.Error?.Code);
        }

        Line("1/2026", "5", "2026-03-10", "true", "a");
        Line("2/2026", "20", "2026-04-02", "false", "b");
        Line("3/2026", "100", "2026-05-20", "true", "b");
        await db.SaveChangesAsync();
        return new World(db, department, form, fields);
    }

    private static async Task<string[]> NumbersAsync(
        World world, DocumentFieldFilter? filter = null, string? sortKey = null, bool descending = false)
    {
        DocumentFieldSort? sort = sortKey is null
            ? null
            : new DocumentFieldSort(world.Fields[sortKey].DocumentFieldId, world.Fields[sortKey].DataType);
        var page = await new DocumentNumberRepository(world.Db).ListDepartmentRegisterPagedAsync(
            new PageRequest(1, 50, null, "registryNumber", descending),
            new DocumentViewAccess(Guid.NewGuid(), null, Now),
            new DepartmentRegisterFilter(
                world.Department.DepartmentId,
                SchemaVersionId: world.Form.SchemaVersionId,
                FieldSort: sort,
                FieldFilters: filter is null ? null : new[] { filter }));
        return page.Items.Select(row => row.Number.OfficialNumberText).ToArray();
    }

    private static DocumentFieldFilter Number(World world, FieldFilterOperator op, decimal value) =>
        new(world.Fields["amount"].DocumentFieldId, DocumentFieldDataType.Number, op, Number: value);

    private static DocumentFieldFilter Day(World world, FieldFilterOperator op, string iso) =>
        new(world.Fields["due"].DocumentFieldId, DocumentFieldDataType.Date, op,
            Date: DateTime.Parse(iso, null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal));

    [Fact]
    public async Task NumberFieldsCompare()
    {
        var world = await CreateWorldAsync();

        Assert.Equal(new[] { "2/2026" }, await NumbersAsync(world, Number(world, FieldFilterOperator.Equals, 20)));
        Assert.Equal(new[] { "2/2026", "3/2026" }, await NumbersAsync(world, Number(world, FieldFilterOperator.GreaterThan, 5)));
        Assert.Equal(new[] { "1/2026", "2/2026", "3/2026" }, await NumbersAsync(world, Number(world, FieldFilterOperator.GreaterThanOrEqual, 5)));
        Assert.Equal(new[] { "1/2026" }, await NumbersAsync(world, Number(world, FieldFilterOperator.LessThan, 20)));
        Assert.Equal(new[] { "1/2026", "2/2026" }, await NumbersAsync(world, Number(world, FieldFilterOperator.LessThanOrEqual, 20)));
    }

    [Fact]
    public async Task DateFieldsCompareByCalendarDay()
    {
        var world = await CreateWorldAsync();

        Assert.Equal(new[] { "2/2026" }, await NumbersAsync(world, Day(world, FieldFilterOperator.Equals, "2026-04-02")));
        Assert.Equal(new[] { "3/2026" }, await NumbersAsync(world, Day(world, FieldFilterOperator.GreaterThan, "2026-04-02")));
        Assert.Equal(new[] { "2/2026", "3/2026" }, await NumbersAsync(world, Day(world, FieldFilterOperator.GreaterThanOrEqual, "2026-04-02")));
        Assert.Equal(new[] { "1/2026" }, await NumbersAsync(world, Day(world, FieldFilterOperator.LessThan, "2026-04-02")));
        Assert.Equal(new[] { "1/2026", "2/2026" }, await NumbersAsync(world, Day(world, FieldFilterOperator.LessThanOrEqual, "2026-04-02")));
    }

    [Fact]
    public async Task BooleanAndSelectFieldsMatchTheirValue()
    {
        var world = await CreateWorldAsync();
        var urgent = world.Fields["urgent"].DocumentFieldId;
        var kind = world.Fields["kind"].DocumentFieldId;

        Assert.Equal(new[] { "1/2026", "3/2026" },
            await NumbersAsync(world, new DocumentFieldFilter(urgent, DocumentFieldDataType.Boolean, FieldFilterOperator.Equals, Boolean: true)));
        Assert.Equal(new[] { "2/2026" },
            await NumbersAsync(world, new DocumentFieldFilter(urgent, DocumentFieldDataType.Boolean, FieldFilterOperator.Equals, Boolean: false)));
        Assert.Equal(new[] { "2/2026", "3/2026" },
            await NumbersAsync(world, new DocumentFieldFilter(kind, DocumentFieldDataType.Select, FieldFilterOperator.Equals, OptionKey: "b")));
        Assert.Empty(await NumbersAsync(world, new DocumentFieldFilter(kind, DocumentFieldDataType.Select, FieldFilterOperator.Equals, OptionKey: "zzz")));
    }

    [Fact]
    public async Task TwoFiltersCombineWithAnd()
    {
        var world = await CreateWorldAsync();
        var page = await new DocumentNumberRepository(world.Db).ListDepartmentRegisterPagedAsync(
            new PageRequest(1, 50, null, "registryNumber", false),
            new DocumentViewAccess(Guid.NewGuid(), null, Now),
            new DepartmentRegisterFilter(
                world.Department.DepartmentId,
                SchemaVersionId: world.Form.SchemaVersionId,
                FieldFilters: new[]
                {
                    new DocumentFieldFilter(world.Fields["kind"].DocumentFieldId, DocumentFieldDataType.Select, FieldFilterOperator.Equals, OptionKey: "b"),
                    Number(world, FieldFilterOperator.GreaterThan, 50)
                }));

        Assert.Equal(new[] { "3/2026" }, page.Items.Select(row => row.Number.OfficialNumberText).ToArray());
    }

    [Fact]
    public async Task SortsByNumberDateAndBooleanFields()
    {
        var world = await CreateWorldAsync();

        Assert.Equal(new[] { "3/2026", "2/2026", "1/2026" }, await NumbersAsync(world, sortKey: "amount", descending: true));
        Assert.Equal(new[] { "1/2026", "2/2026", "3/2026" }, await NumbersAsync(world, sortKey: "due"));
        Assert.Equal(new[] { "2/2026", "1/2026", "3/2026" }, await NumbersAsync(world, sortKey: "urgent"));
    }
}
