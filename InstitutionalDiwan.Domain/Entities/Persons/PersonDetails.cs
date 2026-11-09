namespace InstitutionalDiwan.Domain.Entities.Persons;

/// <summary>Everything that can be recorded about a person. Every value is optional.</summary>
public sealed record PersonDetails(
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
    string? Notes = null);
