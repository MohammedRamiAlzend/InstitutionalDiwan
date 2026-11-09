using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Persons;

/// <summary>A person's full record, for callers holding <c>PERSON_VIEW</c>.</summary>
public sealed record PersonResponse(
    Guid PersonId,
    string DisplayName,
    string? FullName,
    string? FirstName,
    string? LastName,
    string? FatherName,
    string? MotherName,
    string? NationalNumber,
    string? CivilRegistryNumber,
    string? CivilRegistryPlace,
    string? BirthPlace,
    DateOnly? BirthDate,
    int? GenderId,
    int? NationalityId,
    string? Phone,
    string? Address,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static PersonResponse From(Person person) => new(
        person.PersonId,
        person.DisplayName,
        person.FullName,
        person.FirstName,
        person.LastName,
        person.FatherName,
        person.MotherName,
        person.NationalNumber,
        person.CivilRegistryNumber,
        person.CivilRegistryPlace,
        person.BirthPlace,
        person.BirthDate,
        person.GenderId,
        person.NationalityId,
        person.Phone,
        person.Address,
        person.Notes,
        person.CreatedAt,
        person.UpdatedAt);
}

/// <summary>A name match offered while typing: only what is needed to tell two people apart.</summary>
public sealed record PersonSuggestionResponse(
    Guid PersonId,
    string DisplayName,
    int? BirthYear,
    string? NationalNumberLast4,
    DateTime CreatedAt)
{
    private const int VisibleDigits = 4;

    public static PersonSuggestionResponse From(PersonSuggestionRow row) => new(
        row.PersonId,
        row.DisplayName,
        row.BirthDate?.Year,
        row.NationalNumber is { Length: > VisibleDigits } number ? number[^VisibleDigits..] : null,
        row.CreatedAt);
}

public sealed record PersonListItemResponse(
    Guid PersonId,
    string DisplayName,
    string? NationalNumber,
    int DocumentCount,
    DateTime LastActivityAt)
{
    public static PersonListItemResponse From(PersonListRow row) => new(
        row.PersonId, row.DisplayName, row.NationalNumber, row.DocumentCount, row.LastActivityAt);
}

/// <summary>A person named on a document. Holds no identity number, so <c>DOCUMENT_VIEW</c> is enough to read it.</summary>
public sealed record DocumentPersonResponse(
    Guid DocumentPersonId,
    Guid PersonId,
    string DisplayName,
    int RoleId,
    DateTime CreatedAt)
{
    public static DocumentPersonResponse From(DocumentPersonRow row) => new(
        row.DocumentPersonId, row.PersonId, row.DisplayName, row.RoleId, row.CreatedAt);
}
