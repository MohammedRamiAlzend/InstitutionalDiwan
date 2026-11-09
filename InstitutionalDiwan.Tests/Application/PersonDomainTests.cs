using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Tests.Application;

public sealed class PersonDomainTests
{
    private static readonly Guid Actor = Guid.NewGuid();

    private static Document Draft() => Document.CreateDraft(
        "ملف", DocumentType.InternalMemoId, DocumentDirection.InternalId, Guid.NewGuid(), Actor).Value;

    private static void Register(Document document) =>
        Assert.True(document.Register(Guid.NewGuid(), OfficialNumberValue.Create("N-1").Value, Actor).IsSuccess);

    [Theory]
    [InlineData("أحمد", "احمد")]
    [InlineData("إيمان", "ايمان")]
    [InlineData("آمال", "امال")]
    [InlineData("فاطمة", "فاطمه")]
    [InlineData("مصطفى", "مصطفي")]
    [InlineData("مُحَمَّد", "محمد")]
    [InlineData("  أحمد    علي ", "احمد علي")]
    [InlineData("لؤي", "لوي")]
    [InlineData("فائز", "فايز")]
    [InlineData("محـــمد", "محمد")]
    public void Normalize_FoldsSpellingVariants(string input, string expected) =>
        Assert.Equal(expected, PersonNameNormalizer.Normalize(input));

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("َِ")]
    public void Normalize_ReturnsNull_WhenNothingIsLeft(string? input) =>
        Assert.Null(PersonNameNormalizer.Normalize(input));

    [Fact]
    public void Create_AcceptsAFullNameAlone_AndLeavesEveryOtherValueNull()
    {
        var person = Person.Create(new PersonDetails(FullName: " أحمد علي "), Actor).Value;

        Assert.Equal("أحمد علي", person.DisplayName);
        Assert.Equal("احمد علي", person.NormalizedName);
        Assert.Null(person.FirstName);
        Assert.Null(person.NationalNumber);
        Assert.Null(person.BirthDate);
    }

    [Fact]
    public void Create_RejectsAPersonWithNothingToIdentifyThem()
    {
        var result = Person.Create(new PersonDetails(Phone: "0944", Address: "دمشق"), Actor);
        Assert.Equal("Persons.Empty", result.Error.Code);
    }

    [Fact]
    public void DisplayName_FallsBackToThePartsThenTheNationalNumber()
    {
        Assert.Equal(
            "أحمد محمد علي",
            Person.Create(new PersonDetails(FirstName: "أحمد", FatherName: "محمد", LastName: "علي"), Actor).Value.DisplayName);
        Assert.Equal(
            "123",
            Person.Create(new PersonDetails(NationalNumber: "123"), Actor).Value.DisplayName);
    }

    [Fact]
    public void UpdateDetails_ReportsFieldNamesWithoutTheirValues()
    {
        var person = Person.Create(new PersonDetails(FullName: "أحمد"), Actor).Value;
        person.ClearDomainEvents();

        Assert.True(person.UpdateDetails(new PersonDetails(FullName: "أحمد", NationalNumber: "99887766"), Actor).IsSuccess);

        var updated = Assert.IsType<PersonDetailsUpdatedEvent>(Assert.Single(person.DomainEvents));
        Assert.Equal(new[] { nameof(Person.NationalNumber) }, updated.ChangedFields);
        Assert.DoesNotContain("99887766", updated.ToString());
    }

    [Fact]
    public void LinkPerson_RejectsTheSamePersonTwice()
    {
        var document = Draft();
        var personId = Guid.NewGuid();

        Assert.True(document.LinkPerson(personId, DocumentPersonRole.DefendantId, Actor).IsSuccess);
        Assert.Equal(
            "Persons.DuplicateInDocument",
            document.LinkPerson(personId, DocumentPersonRole.PlaintiffId, Actor).Error.Code);
    }

    [Fact]
    public void Persons_AreEditableAfterRegistration()
    {
        var document = Draft();
        Register(document);

        var link = document.LinkPerson(Guid.NewGuid(), DocumentPersonRole.DefendantId, Actor).Value;
        Assert.Equal(
            DocumentPersonRole.PlaintiffId,
            document.ChangePersonRole(link.DocumentPersonId, DocumentPersonRole.PlaintiffId, Actor).Value.RoleId);
        Assert.True(document.UnlinkPerson(link.DocumentPersonId, Actor).IsSuccess);
        Assert.Empty(document.Persons);
    }

    [Fact]
    public void Persons_AreFrozenOnAnArchivedDocument()
    {
        var document = Draft();
        Register(document);
        var link = document.LinkPerson(Guid.NewGuid(), DocumentPersonRole.DefendantId, Actor).Value;
        Assert.True(document.Archive(Actor, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId).IsSuccess);

        Assert.Equal("Correspondence.DocumentArchived", document.LinkPerson(Guid.NewGuid(), 1, Actor).Error.Code);
        Assert.Equal("Correspondence.DocumentArchived", document.ChangePersonRole(link.DocumentPersonId, 2, Actor).Error.Code);
        Assert.Equal("Correspondence.DocumentArchived", document.UnlinkPerson(link.DocumentPersonId, Actor).Error.Code);
    }

    [Fact]
    public void Persons_AreFrozenOnACanceledDocument()
    {
        var document = Draft();
        var link = document.LinkPerson(Guid.NewGuid(), DocumentPersonRole.DefendantId, Actor).Value;
        Assert.True(document.Cancel(Actor, hasActiveMovements: false, "مكرر").IsSuccess);

        Assert.Equal("Persons.DocumentCanceled", document.LinkPerson(Guid.NewGuid(), 1, Actor).Error.Code);
        Assert.Equal("Persons.DocumentCanceled", document.UnlinkPerson(link.DocumentPersonId, Actor).Error.Code);
    }

    [Fact]
    public void ChangeRole_OnAnUnknownLink_IsNotFound()
    {
        Assert.Equal("Persons.LinkNotFound", Draft().ChangePersonRole(Guid.NewGuid(), 1, Actor).Error.Code);
    }

    [Theory]
    [InlineData("01020304050", "4050")]
    [InlineData("1234", null)]
    [InlineData(null, null)]
    public void Suggestion_ExposesOnlyTheLastDigits_AndOnlyWhenSomethingStaysHidden(string? number, string? expected)
    {
        var row = new PersonSuggestionRow(Guid.NewGuid(), "أحمد", new DateOnly(1980, 5, 14), number, DateTime.UtcNow);
        var response = PersonSuggestionResponse.From(row);

        Assert.Equal(expected, response.NationalNumberLast4);
        Assert.Equal(1980, response.BirthYear);
    }
}
