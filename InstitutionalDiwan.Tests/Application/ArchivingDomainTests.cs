using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ArchivingDomainTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private const int Location = ArchiveLocation.PhysicalArchiveId;

    private static Document NewDraft() =>
        Document.CreateDraft(
            "كتاب", DocumentType.InternalMemoId, DocumentDirection.InternalId, Guid.NewGuid(), Actor).Value;

    private static Document NewRegistered()
    {
        var document = NewDraft();
        Assert.True(document.Register(Guid.NewGuid(), OfficialNumberValue.Create("2026/000001").Value, Actor).IsSuccess);
        return document;
    }

    private static Document NewArchived()
    {
        var document = NewRegistered();
        Assert.True(document.Archive(Actor, hasActiveMovements: false, Location).IsSuccess);
        return document;
    }

    [Fact]
    public void Archive_RegisteredDocument_RemembersStatusAndRaisesEventWithLocation()
    {
        var document = NewRegistered();

        var result = document.Archive(Actor, hasActiveMovements: false, Location, "انتهاء المعاملة");

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentStatusType.ArchivedId, document.CurrentStatusId);
        Assert.Equal(DocumentStatusType.RegisteredId, document.StatusBeforeArchiveId);
        Assert.Equal(DocumentStatusType.ArchivedId, document.StatusHistory.Last().ToStatusId);

        var archived = document.DomainEvents.OfType<DocumentArchivedEvent>().Single();
        Assert.Equal(Location, archived.ArchiveLocationId);
        Assert.Equal(DocumentStatusType.RegisteredId, archived.PreviousStatusId);
    }

    [Fact]
    public void Archive_Draft_IsRejected()
    {
        var document = NewDraft();

        var result = document.Archive(Actor, hasActiveMovements: false, Location);

        Assert.Equal(DomainErrors.Correspondence.OnlyRegisteredArchivable, result.Error);
        Assert.Equal(DocumentStatusType.DraftId, document.CurrentStatusId);
    }

    [Fact]
    public void Archive_InMovementWithoutActiveMovement_IsRejected()
    {
        var document = NewRegistered();
        Assert.True(document.MarkInMovement(Actor).IsSuccess);

        var result = document.Archive(Actor, hasActiveMovements: false, Location);

        Assert.Equal(DomainErrors.Correspondence.OnlyRegisteredArchivable, result.Error);
    }

    [Fact]
    public void Archive_WithActiveMovements_IsRejected()
    {
        var result = NewRegistered().Archive(Actor, hasActiveMovements: true, Location);

        Assert.Equal(DomainErrors.Correspondence.HasActiveMovements, result.Error);
    }

    [Fact]
    public void Archive_Canceled_IsRejected()
    {
        var document = NewRegistered();
        Assert.True(document.Cancel(Actor, hasActiveMovements: false, "مكرر").IsSuccess);

        var result = document.Archive(Actor, hasActiveMovements: false, Location);

        Assert.Equal(DomainErrors.Correspondence.CannotArchiveCanceled, result.Error);
    }

    [Fact]
    public void Archive_Twice_IsRejected()
    {
        var result = NewArchived().Archive(Actor, hasActiveMovements: false, Location);

        Assert.Equal(DomainErrors.Correspondence.AlreadyArchived, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Archive_WithoutLocation_IsRejected(int location)
    {
        var document = NewRegistered();

        var result = document.Archive(Actor, hasActiveMovements: false, location);

        Assert.Equal(DomainErrors.Archiving.LocationNotFound, result.Error);
        Assert.Equal(DocumentStatusType.RegisteredId, document.CurrentStatusId);
    }

    [Fact]
    public void Unarchive_RestoresPreviousStatusAndClearsIt()
    {
        var document = NewArchived();

        var result = document.Unarchive(Actor, "  مراجعة قضائية  ");

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentStatusType.RegisteredId, document.CurrentStatusId);
        Assert.Null(document.StatusBeforeArchiveId);
        Assert.Equal("مراجعة قضائية", document.StatusHistory.Last().Reason);
        Assert.Equal(
            DocumentStatusType.RegisteredId,
            document.DomainEvents.OfType<DocumentUnarchivedEvent>().Single().RestoredStatusId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unarchive_WithoutReason_IsRejected(string? reason)
    {
        var document = NewArchived();

        var result = document.Unarchive(Actor, reason);

        Assert.Equal(DomainErrors.Archiving.UnarchiveReasonEmpty, result.Error);
        Assert.Equal(DocumentStatusType.ArchivedId, document.CurrentStatusId);
    }

    [Fact]
    public void Unarchive_NotArchived_IsRejected()
    {
        var result = NewRegistered().Unarchive(Actor, "سبب");

        Assert.Equal(DomainErrors.Correspondence.NotArchived, result.Error);
    }

    [Fact]
    public void ArchivedDocument_RejectsParticipantsAndNumberReplacement_UntilUnarchived()
    {
        var document = NewArchived();
        var target = ParticipantTarget.Person("أحمد").Value;
        var replacement = OfficialNumberValue.Create("2026/000002").Value;

        Assert.Equal(DomainErrors.Correspondence.DocumentArchived, document.EnsureNotArchived().Error);
        Assert.Equal(DomainErrors.Correspondence.DocumentArchived, document.AddParticipant(1, target).Error);
        Assert.Equal(
            DomainErrors.Correspondence.DocumentArchived,
            document.AttachReplacementNumber(Guid.NewGuid(), replacement).Error);

        Assert.True(document.Unarchive(Actor, "سبب").IsSuccess);

        Assert.True(document.EnsureNotArchived().IsSuccess);
        Assert.True(document.AddParticipant(1, target).IsSuccess);
        Assert.True(document.AttachReplacementNumber(Guid.NewGuid(), replacement).IsSuccess);
    }

    [Fact]
    public void ArchiveEvent_Record_EnforcesReasonLength()
    {
        Result<DocumentArchiveEvent> Record(string reason) => DocumentArchiveEvent.Record(
            Guid.NewGuid(), ArchiveEventType.ArchivedId, DocumentStatusType.RegisteredId,
            DocumentStatusType.ArchivedId, Actor, reason, Location);

        Assert.True(Record(new string('س', DocumentArchiveEvent.MaxReasonLength)).IsSuccess);
        Assert.Equal(
            DomainErrors.Archiving.ReasonTooLong,
            Record(new string('س', DocumentArchiveEvent.MaxReasonLength + 1)).Error);
    }

    [Fact]
    public void SavedSearch_Create_ValidatesNameAndCriteriaLimits()
    {
        Error? Create(string? name, string? criteria)
        {
            var result = SavedSearch.Create(Actor, name, criteria);
            return result.IsFailure ? result.Error : null;
        }

        Assert.Null(Create(new string('ن', SavedSearch.MaxNameLength), "{}"));
        Assert.Null(Create("بحث", new string('c', SavedSearch.MaxCriteriaLength)));
        Assert.Equal(DomainErrors.Archiving.SearchNameEmpty, Create(" ", "{}"));
        Assert.Equal(DomainErrors.Archiving.SearchNameTooLong, Create(new string('ن', SavedSearch.MaxNameLength + 1), "{}"));
        Assert.Equal(DomainErrors.Archiving.CriteriaJsonEmpty, Create("بحث", " "));
        Assert.Equal(DomainErrors.Archiving.CriteriaInvalid, Create("بحث", new string('c', SavedSearch.MaxCriteriaLength + 1)));
    }

    [Fact]
    public void SavedSearch_UpdateCriteria_RejectsInvalidValuesWithoutChangingState()
    {
        var search = SavedSearch.Create(Actor, " بحث ", " {} ", isDefault: true).Value;

        var result = search.UpdateCriteria(new string('ن', SavedSearch.MaxNameLength + 1), "{\"a\":1}", false);

        Assert.Equal(DomainErrors.Archiving.SearchNameTooLong, result.Error);
        Assert.Equal("بحث", search.Name);
        Assert.Equal("{}", search.CriteriaJson);
        Assert.True(search.IsDefault);
    }

    [Fact]
    public void SavedSearch_ClearDefault_DropsFlagAndStampsOnlyWhenItChanges()
    {
        var now = new DateTime(2026, 11, 3, 8, 0, 0, DateTimeKind.Utc);
        var search = SavedSearch.Create(Actor, "بحث", "{}", isDefault: true).Value;

        search.ClearDefault(now);

        Assert.False(search.IsDefault);
        Assert.Equal(now, search.UpdatedAt);

        search.ClearDefault(now.AddHours(1));

        Assert.Equal(now, search.UpdatedAt);
    }
}
