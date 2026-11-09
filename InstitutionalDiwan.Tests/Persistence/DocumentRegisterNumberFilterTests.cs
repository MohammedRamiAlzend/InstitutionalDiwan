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

/// <summary>The registry's register-number filter, run against a real SQLite database.</summary>
public sealed class DocumentRegisterNumberFilterTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        DiwanDbContext Db,
        Department Owner,
        Department Receiver,
        NumberingRegistry OwnerBook,
        NumberingRegistry ReceiverBook,
        NumberingRegistry CentralBook,
        User Actor);

    private static async Task<World> CreateWorldAsync()
    {
        var db = TestContextFactory.Create();

        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        db.UserScopeTypes.Add(UserScopeType.Create(UserScopeType.DepartmentId, "Department", "قسم").Value);
        db.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        db.DocumentTypes.Add(DocumentType.Create(DocumentType.InternalMemoId, "Memo", "مذكرة", DocumentDirection.InternalId).Value);
        db.DocumentStatusTypes.Add(DocumentStatusType.Create(DocumentStatusType.DraftId, "Draft", "مسودة").Value);
        db.NumberingResetPolicies.Add(NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);
        db.DocumentNumberScopes.AddRange(
            DocumentNumberScope.Create(DocumentNumberScope.CentralId, "Central", "مركزي").Value,
            DocumentNumberScope.Create(DocumentNumberScope.DepartmentId, "Department", "قسم").Value);
        db.DocumentNumberStatuses.Add(DocumentNumberStatus.Create(DocumentNumberStatus.ActiveId, "Active", "فعال").Value);
        foreach (var id in new[] { DocumentNumberEventType.IssuedId, DocumentNumberEventType.RegisteredInDepartmentId })
            db.DocumentNumberEventTypes.Add(DocumentNumberEventType.Create(id, $"Event {id}", $"حدث {id}").Value);

        var owner = Department.Create("OWNER", "المالك", DepartmentType.SectionId).Value;
        var receiver = Department.Create("RECEIVER", "المستلم", DepartmentType.SectionId).Value;
        db.Departments.AddRange(owner, receiver);

        var actor = User.Create("actor", "hash", owner.DepartmentId).Value;
        db.Users.Add(actor);

        var ownerBook = NumberingRegistry.Create("OWNER-BOOK", "دفتر المالك", departmentId: owner.DepartmentId).Value;
        var receiverBook = NumberingRegistry.Create("RECEIVER-BOOK", "دفتر المستلم", departmentId: receiver.DepartmentId).Value;
        var centralBook = NumberingRegistry.Create("CENTRAL", "مركزي").Value;
        db.NumberingRegistries.AddRange(ownerBook, receiverBook, centralBook);

        await db.SaveChangesAsync();
        return new World(db, owner, receiver, ownerBook, receiverBook, centralBook, actor);
    }

    private static Document AddDocument(World world, string name)
    {
        var document = Document.CreateDraft(
            null, DocumentType.InternalMemoId, DocumentDirection.InternalId, world.Owner.DepartmentId, world.Actor.UserId,
            utcNow: Now, displayName: name).Value;
        world.Db.Documents.Add(document);
        return document;
    }

    private static void AddLine(World world, Document document, Department department, NumberingRegistry book, string number) =>
        Assert.True(document.AddDepartmentNumber(DocumentNumber.CreateDepartmentEntry(
            document.DocumentId, department.DepartmentId, book.RegistryId, number, new DateTime(2026, 3, 9), world.Actor.UserId, utcNow: Now).Value).IsSuccess);

    private static async Task<string[]> FilterAsync(World world, string? number, IReadOnlyCollection<Guid>? scope = null)
    {
        var result = await new DocumentRepository(world.Db).ListPagedAsync(
            new PageRequest(1, 50, null, "title", false),
            new DocumentViewAccess(world.Actor.UserId, scope, Now),
            registerNumber: number);
        return result.Items.Select(document => document.DisplayName).ToArray();
    }

    [Fact]
    public async Task MatchesALineInAnyDepartmentRegister()
    {
        var world = await CreateWorldAsync();
        var document = AddDocument(world, "الأولى");
        AddLine(world, document, world.Owner, world.OwnerBook, "100/2026");
        AddLine(world, document, world.Receiver, world.ReceiverBook, "777/2026");
        AddDocument(world, "الثانية");
        await world.Db.SaveChangesAsync();

        Assert.Equal(new[] { "الأولى" }, await FilterAsync(world, "777"));
        Assert.Equal(new[] { "الأولى" }, await FilterAsync(world, " 100/2026 "));
        Assert.Empty(await FilterAsync(world, "999"));
    }

    [Fact]
    public async Task IgnoresTheCentralNumber()
    {
        var world = await CreateWorldAsync();
        var document = AddDocument(world, "مركزية");
        var central = DocumentNumber.Issue(document.DocumentId, world.CentralBook.RegistryId, 2026, 1, "CENTRAL-555", world.Actor.UserId, utcNow: Now).Value;
        world.Db.DocumentNumbers.Add(central);
        await world.Db.SaveChangesAsync();

        Assert.Empty(await FilterAsync(world, "555"));
    }

    [Fact]
    public async Task BlankValueDoesNotFilter()
    {
        var world = await CreateWorldAsync();
        AddDocument(world, "الأولى");
        await world.Db.SaveChangesAsync();

        Assert.Single(await FilterAsync(world, "   "));
    }

    [Fact]
    public async Task StaysInsideTheCallersAccess()
    {
        var world = await CreateWorldAsync();
        var document = AddDocument(world, "خارج النطاق");
        AddLine(world, document, world.Owner, world.OwnerBook, "100/2026");
        await world.Db.SaveChangesAsync();

        var stranger = User.Create("stranger", "hash", world.Receiver.DepartmentId).Value;
        world.Db.Users.Add(stranger);
        await world.Db.SaveChangesAsync();

        var result = await new DocumentRepository(world.Db).ListPagedAsync(
            new PageRequest(1, 50),
            new DocumentViewAccess(stranger.UserId, new[] { world.Receiver.DepartmentId }, Now),
            registerNumber: "100");

        Assert.Empty(result.Items);
    }
}
