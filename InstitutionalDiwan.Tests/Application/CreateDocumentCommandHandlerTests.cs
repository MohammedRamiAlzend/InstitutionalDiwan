using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class CreateDocumentCommandHandlerTests
{
    private const string RegisterNumber = "17/2026";
    private static readonly DateTime RegisterDate = new(2026, 3, 9);
    private const int TypeId = DocumentType.OutgoingLetterId;
    private const int DirectionId = DocumentDirection.OutgoingId;

    private sealed record World(
        User User,
        FakeUserRepository Users,
        FakeDocumentRepository Documents,
        FakeDocumentTypeRepository Types,
        FakeDocumentNumberRepository Numbers,
        FakeNumberingRegistryRepository Registries,
        FakeUnitOfWork Uow,
        FakeSender Sender);

    /// <summary>The creator's department owns an active register unless <paramref name="departmentRegistry"/> is false.</summary>
    private static World Setup(bool active = true, bool departmentRegistry = true)
    {
        var user = User.Create("author", "hash", Guid.NewGuid(), mustChangePassword: false);
        Assert.True(user.IsSuccess);
        if (!active)
            Assert.True(user.Value.Deactivate().IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);

        var registries = new FakeNumberingRegistryRepository();
        if (departmentRegistry)
        {
            var register = NumberingRegistry.Create(
                "DEPT-BOOK", "دفتر القسم", departmentId: user.Value.PrimaryDepartmentId);
            Assert.True(register.IsSuccess);
            registries.Add(register.Value);
        }

        return new World(user.Value, users, new FakeDocumentRepository(), FakeDocumentTypeRepository.WithDefaultTypes(),
            new FakeDocumentNumberRepository(), registries, new FakeUnitOfWork(), new FakeSender());
    }

    private static CreateDocumentCommandHandler CreateHandler(World w)
        => new(w.Users, w.Documents, w.Types, w.Numbers, w.Registries,
            new FakeDocumentTypeSchemaVersionRepository(), new FakeDocumentFieldOptionResolver(), w.Uow, w.Sender, new DocumentPersonLinker(new FakePersonRepository(), new FakeLookupRepository()));

    private static CreateDocumentCommand Command(
        Guid userId,
        string title = "طلب تجديد",
        int documentTypeId = TypeId,
        int directionId = DirectionId,
        CreateDocumentMovementInput? movement = null)
        => new(userId, title, documentTypeId, RegisterNumber, RegisterDate,
            SubjectSummary: "تجديد رخصة", DirectionId: directionId, Movement: movement);

    [Fact]
    public async Task Handle_CreatesRegisteredDocumentWithSystemGuidNumber()
    {
        var w = Setup();

        var result = await CreateHandler(w).Handle(Command(w.User.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Document.StatusRegistered, result.Value.StatusId);
        Assert.True(Guid.TryParse(result.Value.OfficialNumberText, out _));
        Assert.NotNull(result.Value.RegistrationDate);
        Assert.Equal(w.User.PrimaryDepartmentId, result.Value.OwningDepartmentId);
        Assert.Equal(w.User.UserId, result.Value.CreatedByUserId);

        var stored = Assert.Single(w.Documents.Documents);
        Assert.Equal(2, stored.StatusHistory.Count);

        var issued = Assert.Single(w.Numbers.DocumentNumbers);
        Assert.True(Guid.TryParse(issued.OfficialNumberText, out _));
        Assert.Equal(w.Uow.UtcNow.Year, issued.NumberingYear);
        Assert.Equal(DocumentNumberStatus.ActiveId, issued.StatusId);
    }

    [Fact]
    public async Task Handle_TwoDocuments_GetDifferentSystemNumbers()
    {
        var w = Setup();
        var handler = CreateHandler(w);

        var first = await handler.Handle(Command(w.User.UserId), CancellationToken.None);
        var second = await handler.Handle(Command(w.User.UserId), CancellationToken.None);

        Assert.True(first.IsSuccess && second.IsSuccess);
        Assert.NotEqual(first.Value.OfficialNumberText, second.Value.OfficialNumberText);
    }

    [Fact]
    public async Task Handle_WritesTheTypedNumberAndDateIntoTheCreatorsDepartmentRegister()
    {
        var w = Setup();

        var result = await CreateHandler(w).Handle(Command(w.User.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var line = Assert.IsType<RecordDepartmentRegistrationCommand>(Assert.Single(w.Sender.Requests));
        Assert.Equal(result.Value.DocumentId, line.DocumentId);
        Assert.Equal(w.User.PrimaryDepartmentId, line.DepartmentId);
        Assert.Equal(RegisterNumber, line.RegistryNumberText);
        Assert.Equal(RegisterDate, line.RegistryDate);
        Assert.Equal(w.Registries.Registries.Single(r => r.Code == "DEPT-BOOK").RegistryId, line.RegistryId);
    }

    [Fact]
    public async Task Handle_WithAChosenRegister_WritesTheLineInThatRegister()
    {
        var w = Setup();
        var chosen = Guid.NewGuid();

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId) with { DepartmentRegistryId = chosen }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        var line = Assert.IsType<RecordDepartmentRegistrationCommand>(Assert.Single(w.Sender.Requests));
        Assert.Equal(chosen, line.RegistryId);
    }

    [Fact]
    public async Task Handle_WithoutDepartmentRegister_FailsBeforeCreatingAnything()
    {
        var w = Setup(departmentRegistry: false);

        var result = await CreateHandler(w).Handle(Command(w.User.UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistryNotConfigured.Code, result.Error.Code);
        Assert.Empty(w.Documents.Documents);
        Assert.Empty(w.Numbers.DocumentNumbers);
        Assert.Equal(0, w.Uow.SaveChangesCallCount);
        Assert.Empty(w.Sender.Requests);
    }

    [Fact]
    public async Task Handle_WithMovement_OpensAndSendsItAfterTheRegisterLine()
    {
        var w = Setup();
        var destination = Guid.NewGuid();

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId, movement: new CreateDocumentMovementInput(destination, Instructions: "للمتابعة")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            w.Sender.Requests,
            first => Assert.IsType<RecordDepartmentRegistrationCommand>(first),
            second =>
            {
                var open = Assert.IsType<CreateMovementCommand>(second);
                Assert.Equal(destination, open.DestinationDepartmentId);
                Assert.Equal("للمتابعة", open.Instructions);
            },
            third => Assert.IsType<SendMovementCommand>(third));
    }

    [Fact]
    public async Task Handle_WithoutMovement_DoesNotOpenOne()
    {
        var w = Setup();

        var result = await CreateHandler(w).Handle(Command(w.User.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(w.Sender.Requests, request => request is CreateMovementCommand or SendMovementCommand);
    }

    [Fact]
    public async Task Handle_WhenMovementFails_ReturnsItsError()
    {
        var w = Setup();
        w.Sender.FailWith[typeof(CreateMovementCommand)] = DomainErrors.Movements.CentralDiwanRoutingRequired;

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId, movement: new CreateDocumentMovementInput(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Movements.CentralDiwanRoutingRequired.Code, result.Error.Code);
        Assert.DoesNotContain(w.Sender.Requests, request => request is SendMovementCommand);
    }

    [Fact]
    public async Task Handle_WhenRegisterLineFails_ReturnsItsErrorAndSkipsTheMovement()
    {
        var w = Setup();
        w.Sender.FailWith[typeof(RecordDepartmentRegistrationCommand)] = DomainErrors.Numbering.RegistryNumberTaken;

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId, movement: new CreateDocumentMovementInput(Guid.NewGuid())),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.RegistryNumberTaken.Code, result.Error.Code);
        Assert.DoesNotContain(w.Sender.Requests, request => request is CreateMovementCommand);
    }

    [Fact]
    public async Task Handle_WithUnknownDocumentType_ReturnsValidationError()
    {
        var w = Setup();

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId, documentTypeId: 987), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentTypeNotFound.Code, result.Error.Code);
        Assert.Empty(w.Documents.Documents);
    }

    [Fact]
    public async Task Handle_WithInvalidDirection_ReturnsValidationError()
    {
        var w = Setup();

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId, directionId: 99), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DirectionInvalid.Code, result.Error.Code);
        Assert.Empty(w.Documents.Documents);
    }

    [Fact]
    public async Task Handle_WithWhitespaceTitle_ReturnsValidationError()
    {
        var w = Setup();

        var result = await CreateHandler(w).Handle(
            Command(w.User.UserId, title: "   "), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.TitleEmpty.Code, result.Error.Code);
        Assert.Empty(w.Documents.Documents);
    }

    [Fact]
    public async Task Handle_WhenUserIsMissing_ReturnsNotFound()
    {
        var w = Setup();
        w.Users.Users.Clear();

        var result = await CreateHandler(w).Handle(Command(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserNotFound.Code, result.Error.Code);
        Assert.Equal(0, w.Uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsForbidden()
    {
        var w = Setup(active: false);

        var result = await CreateHandler(w).Handle(Command(w.User.UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.UserInactive.Code, result.Error.Code);
    }
}
