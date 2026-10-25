using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.Identity.ChangePassword;
using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Verifies the deterministic-time seam end to end: every instant a handler causes the domain to
/// stamp comes from <see cref="IUnitOfWork.UtcNow"/>, never from the system clock.
/// </summary>
public sealed class UnitOfWorkClockTests
{
    private static readonly DateTime FixedNow = new(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc);

    private const string Password = "Str0ngP@ss!";

    [Fact]
    public async Task CreatingADocument_StampsEveryWriteWithTheUnitOfWorkInstant()
    {
        var user = User.Create("author", "hash", Guid.NewGuid(), mustChangePassword: false);
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        var documents = new FakeDocumentRepository();
        var numbers = new FakeDocumentNumberRepository();
        var registries = new FakeNumberingRegistryRepository();
        var unitOfWork = new FakeUnitOfWork { UtcNow = FixedNow };

        var handler = new CreateDocumentCommandHandler(
            users,
            documents,
            FakeDocumentTypeRepository.WithDefaultTypes(),
            numbers,
            registries,
            unitOfWork);

        var result = await handler.Handle(
            new CreateDocumentCommand(
                user.Value.UserId,
                "طلب تجديد",
                DocumentType.OutgoingLetterId,
                DocumentDirection.OutgoingId,
                "2026/000001",
                2026),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var document = Assert.Single(documents.Documents);
        Assert.Equal(FixedNow, document.CreatedAt);
        Assert.Equal(FixedNow, document.RegistrationDate);
        Assert.All(document.StatusHistory, history => Assert.Equal(FixedNow, history.ChangedAt));

        var number = Assert.Single(numbers.DocumentNumbers);
        Assert.Equal(FixedNow, number.IssuedAt);
        Assert.All(number.History, history => Assert.Equal(FixedNow, history.OccurredAt));

        var registry = Assert.Single(registries.Registries);
        Assert.Equal(FixedNow, registry.CreatedAt);
    }

    [Fact]
    public async Task LoggingIn_StampsLastLoginWithTheUnitOfWorkInstant()
    {
        var user = User.Create(
            "holder",
            new FakePasswordHasher().Hash(Password),
            Guid.NewGuid(),
            mustChangePassword: false);
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        var unitOfWork = new FakeUnitOfWork { UtcNow = FixedNow };

        var result = await new LoginCommandHandler(
            users,
            new FakeRoleRepository(),
            new FakePasswordHasher(),
            new FakeJwtProvider(),
            unitOfWork).Handle(new LoginCommand("holder", Password), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, user.Value.LastLoginAt);
    }

    [Fact]
    public async Task ChangingAPassword_StampsUpdatedAtWithTheUnitOfWorkInstant()
    {
        var user = User.Create(
            "holder",
            new FakePasswordHasher().Hash(Password),
            Guid.NewGuid(),
            mustChangePassword: true);
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        var unitOfWork = new FakeUnitOfWork { UtcNow = FixedNow };

        var result = await new ChangePasswordCommandHandler(
            users,
            new FakeRoleRepository(),
            new FakePasswordHasher(),
            new FakeJwtProvider(),
            unitOfWork).Handle(
                new ChangePasswordCommand(user.Value.UserId, Password, "N3wStr0ng!P@ss"),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FixedNow, user.Value.UpdatedAt);
    }
}
