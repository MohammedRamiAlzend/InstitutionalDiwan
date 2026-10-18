using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDocumentCommandHandlerTests
{
    private const string Number = "2026/000001";
    private const int Year = 2026;
    private const int TypeId = DocumentType.OutgoingLetterId;
    private const int DirectionId = DocumentDirection.OutgoingId;

    private static (User User, FakeUserRepository Users, FakeDocumentRepository Documents,
            FakeDocumentTypeRepository Types, FakeDocumentNumberRepository Numbers,
            FakeNumberingRegistryRepository Registries, FakeUnitOfWork Uow)
        Setup(bool active = true)
    {
        var user = User.Create("author", "hash", Guid.NewGuid(), mustChangePassword: false);
        Assert.True(user.IsSuccess);
        if (!active)
            Assert.True(user.Value.Deactivate().IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        return (user.Value, users, new FakeDocumentRepository(), FakeDocumentTypeRepository.WithDefaultTypes(),
            new FakeDocumentNumberRepository(), new FakeNumberingRegistryRepository(), new FakeUnitOfWork());
    }

    private static CreateDocumentCommandHandler CreateHandler(
        FakeUserRepository users,
        FakeDocumentRepository documents,
        FakeDocumentTypeRepository types,
        FakeDocumentNumberRepository numbers,
        FakeNumberingRegistryRepository registries,
        FakeUnitOfWork unitOfWork)
        => new(users, documents, types, numbers, registries, unitOfWork);

    private static CreateDocumentCommand Command(
        Guid userId,
        string title = "طلب تجديد",
        int documentTypeId = TypeId,
        int directionId = DirectionId,
        string number = Number,
        int year = Year)
        => new(userId, title, documentTypeId, directionId, number, year, SubjectSummary: "تجديد رخصة");

    [Fact]
    public async Task Handle_WithFreeNumber_CreatesRegisteredDocumentWithOfficialNumber()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Document.StatusRegistered, result.Value.StatusId);
        Assert.Equal(Number, result.Value.OfficialNumberText);
        Assert.NotNull(result.Value.RegistrationDate);
        Assert.Equal(user.PrimaryDepartmentId, result.Value.OwningDepartmentId);
        Assert.Equal(user.UserId, result.Value.CreatedByUserId);

        var stored = Assert.Single(documents.Documents);
        Assert.Equal(result.Value.DocumentId, stored.DocumentId);
        Assert.Equal(2, stored.StatusHistory.Count);

        var issued = Assert.Single(numbers.DocumentNumbers);
        Assert.Equal(Number, issued.OfficialNumberText);
        Assert.Equal(Year, issued.NumberingYear);
        Assert.Equal(DocumentNumberStatus.ActiveId, issued.StatusId);

        Assert.Single(registries.Registries);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenNumberAndYearAlreadyTaken_ReturnsConflict()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var taken = DocumentNumber.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Year, 1, Number, Guid.NewGuid());
        Assert.True(taken.IsSuccess);
        numbers.Add(taken.Value);

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.OfficialNumberTaken.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Empty(documents.Documents);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WithSameNumberInDifferentYear_Succeeds()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var taken = DocumentNumber.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Year, 1, Number, Guid.NewGuid());
        Assert.True(taken.IsSuccess);
        numbers.Add(taken.Value);

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId, year: Year + 1),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            Year + 1,
            Assert.Single(numbers.DocumentNumbers, n => n.NumberingYear == Year + 1).NumberingYear);
    }

    [Fact]
    public async Task Handle_WithDifferentNumberInSameYear_Succeeds()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var taken = DocumentNumber.Issue(
            Guid.NewGuid(), Guid.NewGuid(), Year, 1, Number, Guid.NewGuid());
        Assert.True(taken.IsSuccess);
        numbers.Add(taken.Value);

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId, number: "2026/000002"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, numbers.DocumentNumbers.Count);
    }

    [Fact]
    public async Task Handle_WithExistingActiveRegistry_UsesThatRegistry()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var registry = NumberingRegistry.Create("LETTER-INTERNAL", "سجل الخطابات الداخلية");
        Assert.True(registry.IsSuccess);
        registries.Add(registry.Value);

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(registry.Value.RegistryId, Assert.Single(numbers.DocumentNumbers).RegistryId);
        Assert.Single(registries.Registries);
    }

    [Fact]
    public async Task Handle_WithBlankNumber_ReturnsOfficialNumberEmpty()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId, number: "   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.OfficialNumberEmpty.Code, result.Error.Code);
        Assert.Empty(documents.Documents);
    }

    [Fact]
    public async Task Handle_WithUnknownDocumentType_ReturnsValidationError()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId, documentTypeId: 987),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentTypeNotFound.Code, result.Error.Code);
        Assert.Empty(documents.Documents);
    }

    [Fact]
    public async Task Handle_WithInvalidDirection_ReturnsValidationError()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId, directionId: 99),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DirectionInvalid.Code, result.Error.Code);
        Assert.Empty(documents.Documents);
    }

    [Fact]
    public async Task Handle_WithWhitespaceTitle_ReturnsValidationError()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup();

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId, title: "   "),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.TitleEmpty.Code, result.Error.Code);
        Assert.Empty(documents.Documents);
    }

    [Fact]
    public async Task Handle_WhenUserIsMissing_ReturnsNotFound()
    {
        var (_, users, documents, types, numbers, registries, unitOfWork) = Setup();
        users.Users.Clear();

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsForbidden()
    {
        var (user, users, documents, types, numbers, registries, unitOfWork) = Setup(active: false);

        var result = await CreateHandler(users, documents, types, numbers, registries, unitOfWork).Handle(
            Command(user.UserId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserInactive.Code, result.Error.Code);
    }
}
