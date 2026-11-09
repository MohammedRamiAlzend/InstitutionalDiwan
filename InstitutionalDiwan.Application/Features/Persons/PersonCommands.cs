using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Persons;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Persons;

/// <summary>Replaces a person's recorded details; a value left out is cleared.</summary>
public sealed record UpdatePersonCommand(Guid PersonId, Guid UserId, PersonDetails Details) : ICommand<PersonResponse>;

public sealed record AddDocumentPersonCommand(Guid DocumentId, Guid UserId, DocumentPersonInput Person) : ICommand<DocumentPersonResponse>;

public sealed record ChangeDocumentPersonRoleCommand(Guid DocumentId, Guid DocumentPersonId, Guid UserId, int RoleId) : ICommand<DocumentPersonResponse>;

public sealed record RemoveDocumentPersonCommand(Guid DocumentId, Guid DocumentPersonId, Guid UserId) : ICommand;

public sealed class UpdatePersonCommandValidator : AbstractValidator<UpdatePersonCommand>
{
    private const int MinBirthYear = 1800;

    public UpdatePersonCommandValidator()
    {
        Text(details => details.FullName, Person.MaxFullNameLength);
        Text(details => details.FirstName, Person.MaxNamePartLength);
        Text(details => details.LastName, Person.MaxNamePartLength);
        Text(details => details.FatherName, Person.MaxNamePartLength);
        Text(details => details.MotherName, Person.MaxNamePartLength);
        Text(details => details.NationalNumber, Person.MaxNationalNumberLength);
        Text(details => details.CivilRegistryNumber, Person.MaxCivilRegistryNumberLength);
        Text(details => details.CivilRegistryPlace, Person.MaxPlaceLength);
        Text(details => details.BirthPlace, Person.MaxPlaceLength);
        Text(details => details.Phone, Person.MaxPhoneLength);
        Text(details => details.Address, Person.MaxAddressLength);
        Text(details => details.Notes, Person.MaxNotesLength);

        RuleFor(command => command.Details.BirthDate)
            .Must(date => date!.Value.Year >= MinBirthYear && date.Value.ToDateTime(TimeOnly.MinValue) <= DateTime.UtcNow.AddDays(1))
            .When(command => command.Details.BirthDate.HasValue)
            .WithErrorCode("Persons.BirthDateInvalid");
    }

    private void Text(Func<PersonDetails, string?> value, int maxLength) =>
        RuleFor(command => value(command.Details))
            .MaximumLength(maxLength)
            .OverridePropertyName("Details")
            .WithErrorCode("Persons.ValueTooLong");
}

public sealed class AddDocumentPersonCommandValidator : AbstractValidator<AddDocumentPersonCommand>
{
    public AddDocumentPersonCommandValidator() =>
        RuleFor(command => command.Person).SetValidator(new DocumentPersonInputValidator());
}

internal sealed class UpdatePersonCommandHandler : ICommandHandler<UpdatePersonCommand, PersonResponse>
{
    private readonly IPersonRepository _personRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly PersonAccess _personAccess;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePersonCommandHandler(
        IPersonRepository personRepository,
        ILookupRepository lookupRepository,
        PersonAccess personAccess,
        IUnitOfWork unitOfWork)
    {
        _personRepository = personRepository;
        _lookupRepository = lookupRepository;
        _personAccess = personAccess;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PersonResponse>> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var access = await _personAccess.RequireVisibleAsync(request.PersonId, cancellationToken);
        if (access.IsFailure)
            return Result.Failure<PersonResponse>(access.Error);

        var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken);
        if (person is null)
            return Result.Failure<PersonResponse>(DomainErrors.Persons.NotFound);

        var details = request.Details;

        if (!string.IsNullOrWhiteSpace(details.NationalNumber)
            && await _personRepository.ExistsByNationalNumberAsync(details.NationalNumber.Trim(), person.PersonId, cancellationToken))
        {
            return Result.Failure<PersonResponse>(DomainErrors.Persons.NationalNumberTaken);
        }

        if (details.GenderId is { } genderId && !await IsActiveAsync(Gender.LookupKey, genderId, person.GenderId, cancellationToken))
            return Result.Failure<PersonResponse>(DomainErrors.Persons.GenderNotFound);

        if (details.NationalityId is { } nationalityId && !await IsActiveAsync(Nationality.LookupKey, nationalityId, person.NationalityId, cancellationToken))
            return Result.Failure<PersonResponse>(DomainErrors.Persons.NationalityNotFound);

        var updated = person.UpdateDetails(details, request.UserId, _unitOfWork.UtcNow);
        if (updated.IsFailure)
            return Result.Failure<PersonResponse>(updated.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(PersonResponse.From(person));
    }

    // A value the person already holds stays acceptable after its lookup entry is deactivated.
    private async Task<bool> IsActiveAsync(string lookupKey, int id, int? currentId, CancellationToken cancellationToken)
    {
        if (id == currentId)
            return true;

        var entry = await _lookupRepository.GetByIdAsync(lookupKey, id, cancellationToken);
        return entry is { IsActive: true };
    }
}

/// <summary>Loads a document and confirms the caller may change who is named on it.</summary>
public sealed class DocumentPersonEditGuard
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public DocumentPersonEditGuard(IDocumentRepository documentRepository, IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<Document>> RequireAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
            return Result.Failure<Document>(DomainErrors.Correspondence.DocumentNotFound);

        return await _documentAccessEvaluator.CanAccessDocumentAsync(documentId, AccessLevel.DownloadId, cancellationToken)
            ? Result.Success(document)
            : Result.Failure<Document>(DomainErrors.Correspondence.DocumentAccessDenied);
    }
}

internal sealed class AddDocumentPersonCommandHandler : ICommandHandler<AddDocumentPersonCommand, DocumentPersonResponse>
{
    private readonly DocumentPersonEditGuard _guard;
    private readonly DocumentPersonLinker _linker;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddDocumentPersonCommandHandler(
        DocumentPersonEditGuard guard,
        DocumentPersonLinker linker,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork)
    {
        _guard = guard;
        _linker = linker;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentPersonResponse>> Handle(AddDocumentPersonCommand request, CancellationToken cancellationToken)
    {
        var document = await _guard.RequireAsync(request.DocumentId, cancellationToken);
        if (document.IsFailure)
            return Result.Failure<DocumentPersonResponse>(document.Error);

        var links = await _linker.LinkAsync(document.Value, [request.Person], request.UserId, _unitOfWork.UtcNow, cancellationToken);
        if (links.IsFailure)
            return Result.Failure<DocumentPersonResponse>(links.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await DocumentPersonReader.ReadAsync(_personRepository, request.DocumentId, links.Value[0].DocumentPersonId, cancellationToken);
    }
}

internal sealed class ChangeDocumentPersonRoleCommandHandler : ICommandHandler<ChangeDocumentPersonRoleCommand, DocumentPersonResponse>
{
    private readonly DocumentPersonEditGuard _guard;
    private readonly DocumentPersonLinker _linker;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ChangeDocumentPersonRoleCommandHandler(
        DocumentPersonEditGuard guard,
        DocumentPersonLinker linker,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork)
    {
        _guard = guard;
        _linker = linker;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentPersonResponse>> Handle(ChangeDocumentPersonRoleCommand request, CancellationToken cancellationToken)
    {
        var document = await _guard.RequireAsync(request.DocumentId, cancellationToken);
        if (document.IsFailure)
            return Result.Failure<DocumentPersonResponse>(document.Error);

        var role = await _linker.ResolveRoleAsync(request.RoleId, cancellationToken);
        if (role.IsFailure)
            return Result.Failure<DocumentPersonResponse>(role.Error);

        var changed = document.Value.ChangePersonRole(request.DocumentPersonId, role.Value, request.UserId, _unitOfWork.UtcNow);
        if (changed.IsFailure)
            return Result.Failure<DocumentPersonResponse>(changed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await DocumentPersonReader.ReadAsync(_personRepository, request.DocumentId, request.DocumentPersonId, cancellationToken);
    }
}

internal sealed class RemoveDocumentPersonCommandHandler : ICommandHandler<RemoveDocumentPersonCommand>
{
    private readonly DocumentPersonEditGuard _guard;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveDocumentPersonCommandHandler(DocumentPersonEditGuard guard, IUnitOfWork unitOfWork)
    {
        _guard = guard;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveDocumentPersonCommand request, CancellationToken cancellationToken)
    {
        var document = await _guard.RequireAsync(request.DocumentId, cancellationToken);
        if (document.IsFailure)
            return Result.Failure(document.Error);

        var removed = document.Value.UnlinkPerson(request.DocumentPersonId, request.UserId, _unitOfWork.UtcNow);
        if (removed.IsFailure)
            return removed;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class DocumentPersonReader
{
    public static async Task<Result<DocumentPersonResponse>> ReadAsync(
        IPersonRepository personRepository,
        Guid documentId,
        Guid documentPersonId,
        CancellationToken cancellationToken)
    {
        var rows = await personRepository.ListByDocumentAsync(documentId, cancellationToken);
        var row = rows.FirstOrDefault(candidate => candidate.DocumentPersonId == documentPersonId);
        return row is null
            ? Result.Failure<DocumentPersonResponse>(DomainErrors.Persons.LinkNotFound)
            : Result.Success(DocumentPersonResponse.From(row));
    }
}
