using InstitutionalDiwan.Application.Features.Archiving;
using InstitutionalDiwan.Application.Features.Archiving.SavedSearches;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Runs the saved search use cases against a real SQLite database, because moving the default
/// between searches only works if the writes reach the filtered unique index in the right order.
/// </summary>
public sealed class SavedSearchHandlerTests
{
    private const string Criteria = "{\"search\":\"2026\",\"archiveLocationId\":2}";

    private sealed record World(DiwanDbContext Db, User Owner, User Other)
    {
        public CreateSavedSearchCommandHandler CreateHandler() => new(new SavedSearchRepository(Db), Db);
        public UpdateSavedSearchCommandHandler UpdateHandler() => new(new SavedSearchRepository(Db), Db);
        public DeleteSavedSearchCommandHandler DeleteHandler() => new(new SavedSearchRepository(Db), Db);
        public ListSavedSearchesQueryHandler ListHandler() => new(new SavedSearchRepository(Db));

        public async Task<SavedSearchResponse> CreateAsync(string name, bool isDefault = false, Guid? userId = null)
        {
            var result = await CreateHandler().Handle(
                new CreateSavedSearchCommand(userId ?? Owner.UserId, name, Criteria, isDefault),
                CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error?.Code);
            return result.Value;
        }

        public Task<List<string>> DefaultNamesAsync(Guid userId) =>
            Db.SavedSearches.AsNoTracking()
                .Where(search => search.UserId == userId && search.IsDefault)
                .Select(search => search.Name)
                .ToListAsync();
    }

    private static async Task<World> CreateWorldAsync()
    {
        var db = TestContextFactory.Create();
        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        db.UserScopeTypes.Add(UserScopeType.Create(UserScopeType.DepartmentId, "Department", "دائرة").Value);
        var department = Department.Create("DEP", "دائرة", DepartmentType.SectionId).Value;
        db.Departments.Add(department);
        var owner = User.Create("owner", "hash", department.DepartmentId).Value;
        var other = User.Create("other", "hash", department.DepartmentId).Value;
        db.Users.AddRange(owner, other);
        await db.SaveChangesAsync();
        return new World(db, owner, other);
    }

    [Fact]
    public async Task Create_NewDefault_ReplacesTheCurrentDefault()
    {
        var world = await CreateWorldAsync();
        await world.CreateAsync("أول", isDefault: true);

        var second = await world.CreateAsync("ثان", isDefault: true);

        Assert.True(second.IsDefault);
        Assert.Equal(new[] { "ثان" }, await world.DefaultNamesAsync(world.Owner.UserId));
    }

    [Fact]
    public async Task Create_DefaultOfAnotherUser_IsUntouched()
    {
        var world = await CreateWorldAsync();
        await world.CreateAsync("لغيره", isDefault: true, userId: world.Other.UserId);

        await world.CreateAsync("لي", isDefault: true);

        Assert.Equal(new[] { "لغيره" }, await world.DefaultNamesAsync(world.Other.UserId));
        Assert.Equal(new[] { "لي" }, await world.DefaultNamesAsync(world.Owner.UserId));
    }

    [Fact]
    public async Task Create_DuplicateName_IsRejected_ButAnotherUserMayUseIt()
    {
        var world = await CreateWorldAsync();
        await world.CreateAsync("بحث");

        var duplicate = await world.CreateHandler().Handle(
            new CreateSavedSearchCommand(world.Owner.UserId, " بحث ", Criteria), CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.SearchNameTaken.Code, duplicate.Error.Code);
        await world.CreateAsync("بحث", userId: world.Other.UserId);
    }

    [Fact]
    public async Task Create_BeyondTheLimit_IsRejected()
    {
        var world = await CreateWorldAsync();
        for (var i = 0; i < SavedSearch.MaxPerUser; i++)
            await world.CreateAsync($"بحث {i}");

        var result = await world.CreateHandler().Handle(
            new CreateSavedSearchCommand(world.Owner.UserId, "زائد", Criteria), CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.SearchLimitReached.Code, result.Error.Code);
    }

    [Theory]
    [InlineData("{}", true)]
    [InlineData(Criteria, true)]
    [InlineData("{\"sortBy\":\"title\",\"sortDescending\":false}", true)]
    [InlineData("{\"archivedFromUtc\":\"2026-01-01T00:00:00Z\",\"owningDepartmentId\":\"e7a62f43-9b82-4d01-8c2e-7ab91e506d42\"}", true)]
    [InlineData("{\"search\":\"2026\",\"sort\":\"title\",\"dir\":\"asc\",\"owningDepartmentId\":\"e7a62f43-9b82-4d01-8c2e-7ab91e506d42\",\"documentTypeId\":\"1\",\"archiveLocationId\":\"2\",\"archivedFrom\":\"2026-01-01\",\"archivedTo\":\"2026-12-31\",\"f\":\"kind:eq:other\\nurgent:eq:true\"}", true)]
    [InlineData("{\"sort\":\"password\"}", false)]
    [InlineData("{\"dir\":\"sideways\"}", false)]
    [InlineData("{\"archivedFrom\":\"yesterday\"}", false)]
    [InlineData("not json", false)]
    [InlineData("[]", false)]
    [InlineData("null", false)]
    [InlineData("{\"unknownField\":1}", false)]
    [InlineData("{\"sortBy\":\"password\"}", false)]
    [InlineData("{\"archiveLocationId\":\"abc\"}", false)]
    public void Criteria_AcceptsOnlyTheKnownShape(string json, bool valid) =>
        Assert.Equal(valid, ArchiveSearchCriteria.IsValid(json));

    [Fact]
    public async Task Create_InvalidCriteria_IsRejected()
    {
        var world = await CreateWorldAsync();

        var result = await world.CreateHandler().Handle(
            new CreateSavedSearchCommand(world.Owner.UserId, "بحث", "{\"unknownField\":1}"), CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.CriteriaInvalid.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_MovesTheDefaultFromAnotherSearch()
    {
        var world = await CreateWorldAsync();
        await world.CreateAsync("أول", isDefault: true);
        var second = await world.CreateAsync("ثان");

        var result = await world.UpdateHandler().Handle(
            new UpdateSavedSearchCommand(second.SavedSearchId, world.Owner.UserId, "ثان معدل", "{}", IsDefault: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal("ثان معدل", result.Value.Name);
        Assert.Equal("{}", result.Value.CriteriaJson);
        Assert.NotNull(result.Value.UpdatedAt);
        Assert.Equal(new[] { "ثان معدل" }, await world.DefaultNamesAsync(world.Owner.UserId));
    }

    [Fact]
    public async Task Update_TheCurrentDefault_KeepingItsOwnNameAndFlag_Succeeds()
    {
        var world = await CreateWorldAsync();
        var search = await world.CreateAsync("بحث", isDefault: true);

        var result = await world.UpdateHandler().Handle(
            new UpdateSavedSearchCommand(search.SavedSearchId, world.Owner.UserId, "بحث", "{}", IsDefault: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(new[] { "بحث" }, await world.DefaultNamesAsync(world.Owner.UserId));
    }

    [Fact]
    public async Task Update_ToANameAlreadyUsed_IsRejected()
    {
        var world = await CreateWorldAsync();
        await world.CreateAsync("أول");
        var second = await world.CreateAsync("ثان");

        var result = await world.UpdateHandler().Handle(
            new UpdateSavedSearchCommand(second.SavedSearchId, world.Owner.UserId, "أول", "{}", IsDefault: false),
            CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.SearchNameTaken.Code, result.Error.Code);
    }

    [Fact]
    public async Task AnotherUsersSearch_AnswersAsNotFound_AndStaysUnchanged()
    {
        var world = await CreateWorldAsync();
        var search = await world.CreateAsync("بحث", userId: world.Other.UserId);

        var update = await world.UpdateHandler().Handle(
            new UpdateSavedSearchCommand(search.SavedSearchId, world.Owner.UserId, "مختطف", "{}", IsDefault: false),
            CancellationToken.None);
        var delete = await world.DeleteHandler().Handle(
            new DeleteSavedSearchCommand(search.SavedSearchId, world.Owner.UserId), CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.SearchNotFound.Code, update.Error.Code);
        Assert.Equal(DomainErrors.Archiving.SearchNotFound.Code, delete.Error.Code);
        Assert.Equal("بحث", (await world.Db.SavedSearches.AsNoTracking().SingleAsync()).Name);
    }

    [Fact]
    public async Task Delete_RemovesTheSearch()
    {
        var world = await CreateWorldAsync();
        var search = await world.CreateAsync("بحث");

        var result = await world.DeleteHandler().Handle(
            new DeleteSavedSearchCommand(search.SavedSearchId, world.Owner.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(world.Db.SavedSearches);
    }

    [Fact]
    public async Task List_ReturnsOnlyTheCallersSearchesByName()
    {
        var world = await CreateWorldAsync();
        await world.CreateAsync("باء");
        await world.CreateAsync("ألف");
        await world.CreateAsync("لغيره", userId: world.Other.UserId);

        var result = await world.ListHandler().Handle(new ListSavedSearchesQuery(world.Owner.UserId), CancellationToken.None);

        Assert.Equal(new[] { "ألف", "باء" }, result.Value.Select(search => search.Name));
    }

    [Theory]
    [InlineData("", "{}", false)]
    [InlineData("بحث", "", false)]
    [InlineData("بحث", "{}", true)]
    public void Validators_RequireNameAndCriteria(string name, string criteria, bool valid)
    {
        Assert.Equal(valid, new CreateSavedSearchCommandValidator()
            .Validate(new CreateSavedSearchCommand(Guid.NewGuid(), name, criteria)).IsValid);
        Assert.Equal(valid, new UpdateSavedSearchCommandValidator()
            .Validate(new UpdateSavedSearchCommand(Guid.NewGuid(), Guid.NewGuid(), name, criteria, false)).IsValid);
    }
}
