using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Domain.Entities.Persons;

namespace InstitutionalDiwan.Server.Contracts.Persons;

/// <summary>One person named on a document: <c>personId</c> of an existing person, or <c>fullName</c> of a new one.</summary>
public sealed record DocumentPersonRequest(Guid? PersonId = null, string? FullName = null, int? RoleId = null);

/// <summary>Payload of <c>PUT /api/documents/{id}/persons/{documentPersonId}</c>.</summary>
public sealed record ChangeDocumentPersonRoleRequest(int RoleId);

/// <summary>Payload of <c>PUT /api/persons/{id}</c>. A value left out is cleared.</summary>
public sealed record UpdatePersonRequest(
    string? FullName = null,
    string? FirstName = null,
    string? LastName = null,
    string? FatherName = null,
    string? MotherName = null,
    string? NationalNumber = null,
    string? CivilRegistryNumber = null,
    string? CivilRegistryPlace = null,
    string? BirthPlace = null,
    DateOnly? BirthDate = null,
    int? GenderId = null,
    int? NationalityId = null,
    string? Phone = null,
    string? Address = null,
    string? Notes = null)
{
    public PersonDetails ToDetails() => new(
        FullName, FirstName, LastName, FatherName, MotherName, NationalNumber, CivilRegistryNumber,
        CivilRegistryPlace, BirthPlace, BirthDate, GenderId, NationalityId, Phone, Address, Notes);
}

public static class DocumentPersonRequestExtensions
{
    public static IReadOnlyList<DocumentPersonInput>? ToInputs(this IReadOnlyList<DocumentPersonRequest>? requests) =>
        requests?.Select(request => request.ToInput()).ToList();

    public static DocumentPersonInput ToInput(this DocumentPersonRequest request) =>
        new(request.PersonId, request.FullName, request.RoleId);
}
