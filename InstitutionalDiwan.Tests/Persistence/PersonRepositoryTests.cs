using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Runs the person queries against a real SQLite database: everything reached from a person's name
/// stops at the documents the caller may view, and the name suggestions deliberately do not.
/// </summary>
public sealed class PersonRepositoryTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        DiwanDbContext Db,
        PersonRepository Persons,
        Department Mine,
        Department Other,
        User Author,
        User Stranger)
    {
        public DocumentViewAccess MyAccess => new(Stranger.UserId, new[] { Mine.DepartmentId }, Now);

        public DocumentViewAccess OutsiderAccess => new(Stranger.UserId, new[] { Guid.NewGuid() }, Now);
    }

    private static async Task<World> CreateWorldAsync()
    {
        var db = TestContextFactory.Create();

        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        db.UserScopeTypes.Add(UserScopeType.Create(UserScopeType.DepartmentId, "Department", "قسم").Value);
        db.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        db.DocumentTypes.Add(DocumentType.Create(DocumentType.InternalMemoId, "Memo", "مذكرة", DocumentDirection.InternalId).Value);
        foreach (var id in new[] { DocumentConfidentialityLevel.NormalId, DocumentConfidentialityLevel.SecretId })
            db.DocumentConfidentialityLevels.Add(DocumentConfidentialityLevel.Create(id, $"Level {id}", $"مستوى {id}").Value);
        foreach (var id in new[] { DocumentStatusType.DraftId, DocumentStatusType.RegisteredId, DocumentStatusType.InMovementId, DocumentStatusType.ArchivedId, DocumentStatusType.CanceledId })
            db.DocumentStatusTypes.Add(DocumentStatusType.Create(id, $"Status {id}", $"حالة {id}").Value);
        db.MovementTypes.Add(MovementType.Create(MovementType.ReferralId, "Referral", "إحالة").Value);
        foreach (var id in new[] { MovementStatus.DraftId, MovementStatus.SentId })
            db.MovementStatuses.Add(MovementStatus.Create(id, $"Status {id}", $"حالة {id}").Value);
        foreach (var id in new[] { MovementStepStatus.PendingId, MovementStepStatus.SentId })
            db.MovementStepStatuses.Add(MovementStepStatus.Create(id, $"Step {id}", $"خطوة {id}").Value);
        foreach (var id in new[] { MovementEventType.CreatedId, MovementEventType.SentId })
            db.MovementEventTypes.Add(MovementEventType.Create(id, $"Event {id}", $"حدث {id}").Value);
        db.ArchiveEventTypes.Add(ArchiveEventType.Create(ArchiveEventType.ArchivedId, "Archived", "أرشفة").Value);
        db.ArchiveLocations.Add(ArchiveLocation.Create(ArchiveLocation.PhysicalArchiveId, "Physical", "ورقي").Value);

        var defendant = DocumentPersonRole.Create(DocumentPersonRole.DefendantId, "Defendant", "مدعى عليه").Value;
        defendant.MarkDefault();
        db.DocumentPersonRoles.AddRange(defendant, DocumentPersonRole.Create(DocumentPersonRole.PlaintiffId, "Plaintiff", "مدعٍ").Value);

        var mine = Department.Create("MINE", "قسمي", DepartmentType.SectionId).Value;
        var other = Department.Create("OTHER", "قسم آخر", DepartmentType.SectionId).Value;
        db.Departments.AddRange(mine, other);

        var author = User.Create("author", "hash", other.DepartmentId).Value;
        var stranger = User.Create("stranger", "hash", mine.DepartmentId).Value;
        db.Users.AddRange(author, stranger);

        await db.SaveChangesAsync();
        return new World(db, new PersonRepository(db), mine, other, author, stranger);
    }

    private static Person AddPerson(World world, string name, string? nationalNumber = null)
    {
        var person = Person.Create(new PersonDetails(FullName: name, NationalNumber: nationalNumber), world.Author.UserId, Now).Value;
        world.Db.Persons.Add(person);
        return person;
    }

    private static Document AddDocument(World world, Guid departmentId, string title, int confidentiality = DocumentConfidentialityLevel.NormalId)
    {
        var document = Document.CreateDraft(
            title, DocumentType.InternalMemoId, DocumentDirection.InternalId, departmentId, world.Author.UserId,
            confidentialityLevelId: confidentiality, utcNow: Now).Value;
        world.Db.Documents.Add(document);
        return document;
    }

    private static void Link(World world, Document document, Person person, int roleId = DocumentPersonRole.DefendantId) =>
        Assert.True(document.LinkPerson(person.PersonId, roleId, world.Author.UserId, Now).IsSuccess);

    private static void Register(World world, Document document) =>
        Assert.True(document.Register(
            Guid.NewGuid(), OfficialNumberValue.Create($"N-{Guid.NewGuid():N}"[..12]).Value, world.Author.UserId, utcNow: Now).IsSuccess);

    private static PageRequest AllRows() => new(1, 100, null, null, true);

    [Fact]
    public async Task EverythingReachedFromAPerson_StopsAtViewableDocuments()
    {
        var world = await CreateWorldAsync();
        var person = AddPerson(world, "أحمد محمد علي");

        var visible = AddDocument(world, world.Mine.DepartmentId, "في نطاقي");
        var otherDepartment = AddDocument(world, world.Other.DepartmentId, "قسم آخر");
        var secret = AddDocument(world, world.Mine.DepartmentId, "سرية", DocumentConfidentialityLevel.SecretId);
        Link(world, visible, person);
        Link(world, otherDepartment, person);
        Link(world, secret, person);
        await world.Db.SaveChangesAsync();

        var list = await world.Persons.ListPagedAsync(AllRows(), world.MyAccess);
        var row = Assert.Single(list.Items);
        Assert.Equal(1, row.DocumentCount);

        var documents = await world.Persons.ListDocumentsAsync(person.PersonId, AllRows(), world.MyAccess);
        Assert.Equal(1, documents.TotalCount);
        Assert.Equal(visible.DocumentId, Assert.Single(documents.Items).DocumentId);

        var timeline = await world.Persons.ListTimelineAsync(person.PersonId, AllRows(), world.MyAccess);
        Assert.All(timeline.Items, item => Assert.Equal(visible.DocumentId, item.DocumentId));

        Assert.True(await world.Persons.IsVisibleAsync(person.PersonId, world.MyAccess));
        Assert.False(await world.Persons.IsVisibleAsync(person.PersonId, world.OutsiderAccess));
        Assert.Empty((await world.Persons.ListPagedAsync(AllRows(), world.OutsiderAccess)).Items);
    }

    [Fact]
    public async Task Suggest_IgnoresAccess_AndFoldsArabicSpelling()
    {
        var world = await CreateWorldAsync();
        AddPerson(world, "أحمد سعيد الخطيبَة");
        AddPerson(world, "خالد عمر");
        await world.Db.SaveChangesAsync();

        var tokens = PersonNameNormalizer.Tokens("احمد الخطيبة");
        var match = Assert.Single(await world.Persons.SuggestAsync(tokens, 10));
        Assert.Equal("أحمد سعيد الخطيبَة", match.DisplayName);
    }

    [Fact]
    public async Task ListPaged_SearchesTheNationalNumberWhenAllDigits()
    {
        var world = await CreateWorldAsync();
        var numbered = AddPerson(world, "سامر", "01020304050");
        var unnumbered = AddPerson(world, "ماهر");
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");
        Link(world, document, numbered);
        Link(world, document, unnumbered);
        await world.Db.SaveChangesAsync();

        var result = await world.Persons.ListPagedAsync(new PageRequest(1, 20, "30405", null, true), world.MyAccess);
        Assert.Equal(numbered.PersonId, Assert.Single(result.Items).PersonId);
    }

    [Fact]
    public async Task NationalNumber_IsUniqueOnlyWhenPresent()
    {
        var world = await CreateWorldAsync();
        AddPerson(world, "الأول");
        AddPerson(world, "الثاني");
        AddPerson(world, "الثالث", "111");
        await world.Db.SaveChangesAsync();

        AddPerson(world, "الرابع", "111");
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task ListDocuments_ReportsWhereEachDocumentStandsNow()
    {
        var world = await CreateWorldAsync();
        var person = AddPerson(world, "أحمد");

        var resting = AddDocument(world, world.Mine.DepartmentId, "ثابتة");
        var moving = AddDocument(world, world.Mine.DepartmentId, "متحركة");
        var shelved = AddDocument(world, world.Mine.DepartmentId, "مؤرشفة");
        foreach (var document in new[] { resting, moving, shelved })
        {
            Link(world, document, person);
            Register(world, document);
        }

        var movement = DocumentMovement.Create(moving.DocumentId, world.Mine.DepartmentId, world.Other.DepartmentId, world.Author.UserId, utcNow: Now).Value;
        Assert.True(movement.Send(world.Author.UserId, utcNow: Now.AddMinutes(5)).IsSuccess);
        world.Db.DocumentMovements.Add(movement);

        Assert.True(shelved.Archive(world.Author.UserId, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId, utcNow: Now.AddMinutes(1)).IsSuccess);
        world.Db.DocumentArchiveEvents.Add(DocumentArchiveEvent.Record(
            shelved.DocumentId, ArchiveEventType.ArchivedId, DocumentStatusType.RegisteredId, DocumentStatusType.ArchivedId,
            world.Author.UserId, archiveLocationId: ArchiveLocation.PhysicalArchiveId, utcNow: Now.AddMinutes(1)).Value);
        await world.Db.SaveChangesAsync();

        var rows = (await world.Persons.ListDocumentsAsync(person.PersonId, AllRows(), world.MyAccess)).Items
            .ToDictionary(row => row.DocumentId);

        Assert.Equal(world.Mine.DepartmentId, rows[resting.DocumentId].CurrentDepartmentId);
        Assert.Null(rows[resting.DocumentId].ActiveMovementStatusId);

        Assert.Equal(world.Other.DepartmentId, rows[moving.DocumentId].CurrentDepartmentId);
        Assert.Equal(PersonTimelineKind.Movement, rows[moving.DocumentId].LastActionKind);

        Assert.Null(rows[shelved.DocumentId].CurrentDepartmentId);
        Assert.Equal(ArchiveLocation.PhysicalArchiveId, rows[shelved.DocumentId].CurrentArchiveLocationId);
    }

    [Fact]
    public async Task Timeline_MergesEverySource_NewestFirst_AndPagesAcrossThem()
    {
        var world = await CreateWorldAsync();
        var person = AddPerson(world, "أحمد");
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");
        Link(world, document, person);
        Register(world, document);

        var movement = DocumentMovement.Create(document.DocumentId, world.Mine.DepartmentId, world.Other.DepartmentId, world.Author.UserId, utcNow: Now.AddMinutes(1)).Value;
        Assert.True(movement.Send(world.Author.UserId, "عاجل", utcNow: Now.AddMinutes(2)).IsSuccess);
        world.Db.DocumentMovements.Add(movement);
        await world.Db.SaveChangesAsync();

        var all = await world.Persons.ListTimelineAsync(person.PersonId, AllRows(), world.MyAccess);
        Assert.Contains(all.Items, item => item.Kind == PersonTimelineKind.Status);
        Assert.Contains(all.Items, item => item.Kind == PersonTimelineKind.Person && item.RoleId == DocumentPersonRole.DefendantId);

        var sent = Assert.Single(all.Items, item => item.MovementEventTypeId == MovementEventType.SentId);
        Assert.Equal(PersonTimelineKind.Movement, sent.Kind);
        Assert.Equal(world.Other.DepartmentId, sent.ToDepartmentId);
        Assert.Equal(all.Items.OrderByDescending(item => item.OccurredAt).Select(item => item.OccurredAt), all.Items.Select(item => item.OccurredAt));

        var firstPage = await world.Persons.ListTimelineAsync(person.PersonId, new PageRequest(1, 2), world.MyAccess);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(all.TotalCount, firstPage.TotalCount);
    }

    [Fact]
    public async Task ArchiveTransition_AppearsOnce()
    {
        var world = await CreateWorldAsync();
        var person = AddPerson(world, "أحمد");
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");
        Link(world, document, person);
        Register(world, document);
        Assert.True(document.Archive(world.Author.UserId, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId, utcNow: Now.AddMinutes(1)).IsSuccess);
        world.Db.DocumentArchiveEvents.Add(DocumentArchiveEvent.Record(
            document.DocumentId, ArchiveEventType.ArchivedId, DocumentStatusType.RegisteredId, DocumentStatusType.ArchivedId,
            world.Author.UserId, archiveLocationId: ArchiveLocation.PhysicalArchiveId, utcNow: Now.AddMinutes(1)).Value);
        await world.Db.SaveChangesAsync();

        var timeline = await world.Persons.ListTimelineAsync(person.PersonId, AllRows(), world.MyAccess);
        Assert.Single(timeline.Items, item => item.Kind == PersonTimelineKind.Archive);
        Assert.DoesNotContain(timeline.Items, item => item.ToStatusId == DocumentStatusType.ArchivedId);
    }

    [Fact]
    public async Task Linker_UsesTheCatalogDefault_ThatTheLookupsScreenCanChange()
    {
        var world = await CreateWorldAsync();
        var lookups = new LookupRepository(world.Db);
        var linker = new DocumentPersonLinker(world.Persons, lookups);

        var first = AddDocument(world, world.Mine.DepartmentId, "أولى");
        var defaulted = await linker.LinkAsync(first, [new DocumentPersonInput(FullName: "أحمد")], world.Author.UserId, Now, CancellationToken.None);
        Assert.Equal(DocumentPersonRole.DefendantId, Assert.Single(defaulted.Value).RoleId);
        await world.Db.SaveChangesAsync();

        Assert.True((await lookups.SetDefaultStateAsync(DocumentPersonRole.LookupKey, DocumentPersonRole.PlaintiffId, true)).IsSuccess);
        await world.Db.SaveChangesAsync();

        var second = AddDocument(world, world.Mine.DepartmentId, "ثانية");
        var changed = await linker.LinkAsync(second, [new DocumentPersonInput(FullName: "خالد")], world.Author.UserId, Now, CancellationToken.None);
        Assert.Equal(DocumentPersonRole.PlaintiffId, Assert.Single(changed.Value).RoleId);
    }

    [Fact]
    public async Task Linker_ReusesOnlyAnExplicitlyChosenPerson()
    {
        var world = await CreateWorldAsync();
        var existing = AddPerson(world, "أحمد محمد");
        await world.Db.SaveChangesAsync();

        var linker = new DocumentPersonLinker(world.Persons, new LookupRepository(world.Db));
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");

        var result = await linker.LinkAsync(
            document,
            [new DocumentPersonInput(PersonId: existing.PersonId), new DocumentPersonInput(FullName: "أحمد محمد")],
            world.Author.UserId,
            Now,
            CancellationToken.None);
        await world.Db.SaveChangesAsync();

        Assert.Equal(2, result.Value.Count);
        Assert.Equal(2, await world.Db.Persons.CountAsync());
    }

    [Fact]
    public async Task Linker_RejectsAnyFailure_BeforeAnythingIsSaved()
    {
        var world = await CreateWorldAsync();
        var linker = new DocumentPersonLinker(world.Persons, new LookupRepository(world.Db));
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");

        var duplicate = await linker.LinkAsync(
            document,
            [new DocumentPersonInput(FullName: "أحمد"), new DocumentPersonInput(FullName: "احمد")],
            world.Author.UserId,
            Now,
            CancellationToken.None);
        Assert.Equal("Persons.DuplicateInDocument", duplicate.Error.Code);

        var badRole = await linker.LinkAsync(
            document, [new DocumentPersonInput(FullName: "خالد", RoleId: 999)], world.Author.UserId, Now, CancellationToken.None);
        Assert.Equal("Persons.RoleNotFound", badRole.Error.Code);

        var neither = await linker.LinkAsync(document, [new DocumentPersonInput()], world.Author.UserId, Now, CancellationToken.None);
        Assert.Equal("Persons.ReferenceInvalid", neither.Error.Code);
    }

    [Fact]
    public async Task ListByDocument_ReturnsNamesAndRoles_InTheOrderAdded()
    {
        var world = await CreateWorldAsync();
        var first = AddPerson(world, "الأول");
        var second = AddPerson(world, "الثاني");
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");
        Assert.True(document.LinkPerson(second.PersonId, DocumentPersonRole.PlaintiffId, world.Author.UserId, Now.AddMinutes(1)).IsSuccess);
        Assert.True(document.LinkPerson(first.PersonId, DocumentPersonRole.DefendantId, world.Author.UserId, Now).IsSuccess);
        await world.Db.SaveChangesAsync();

        var rows = await world.Persons.ListByDocumentAsync(document.DocumentId);

        Assert.Equal(new[] { "الأول", "الثاني" }, rows.Select(row => row.DisplayName));
        Assert.Equal(DocumentPersonRole.PlaintiffId, rows[1].RoleId);
    }

    [Fact]
    public async Task Unlink_DeletesTheLinkButKeepsThePerson()
    {
        var world = await CreateWorldAsync();
        var person = AddPerson(world, "أحمد");
        var document = AddDocument(world, world.Mine.DepartmentId, "ملف");
        Link(world, document, person);
        await world.Db.SaveChangesAsync();

        var link = Assert.Single(document.Persons);
        Assert.True(document.UnlinkPerson(link.DocumentPersonId, world.Author.UserId, Now).IsSuccess);
        await world.Db.SaveChangesAsync();

        Assert.Empty(await world.Db.DocumentPersons.ToListAsync());
        Assert.Equal(1, await world.Db.Persons.CountAsync());
    }
}
