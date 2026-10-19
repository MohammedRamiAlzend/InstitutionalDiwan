using InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ExternalOrganizationHandlerTests
{
    private static FakeLookupRepository CreateLookups()
    {
        var lookups = new FakeLookupRepository();
        lookups.Add(
            "organization-types",
            OrganizationType.Create(OrganizationType.GovernmentId, "Government", "حكومي").Value);
        return lookups;
    }

    private static ExternalOrganization CreateOrganization(
        string code = "MOI",
        bool active = true)
    {
        var organization = ExternalOrganization.Create(
            code, "وزارة الداخلية", OrganizationType.GovernmentId);
        Assert.True(organization.IsSuccess);

        if (!active)
            organization.Value.Deactivate();

        return organization.Value;
    }

    private static CreateExternalOrganizationCommandHandler CreateHandler(
        FakeExternalOrganizationRepository organizations,
        FakeLookupRepository lookups,
        FakeUnitOfWork unitOfWork)
        => new(organizations, lookups, unitOfWork);

    [Fact]
    public async Task Create_WithValidPayload_PersistsOrganization()
    {
        var organizations = new FakeExternalOrganizationRepository();
        var unitOfWork = new FakeUnitOfWork();

        var result = await CreateHandler(organizations, CreateLookups(), unitOfWork)
            .Handle(new CreateExternalOrganizationCommand(
                "moi",
                "وزارة الداخلية",
                OrganizationType.GovernmentId,
                "Ministry of Interior",
                "+963 11 123456"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("MOI", result.Value.Code);
        Assert.Equal("Ministry of Interior", result.Value.NameEn);
        Assert.True(result.Value.IsActive);
        Assert.Single(organizations.Organizations);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Create_WithDuplicateCode_ReturnsConflict()
    {
        var organizations = new FakeExternalOrganizationRepository();
        organizations.Add(CreateOrganization("MOI"));

        var result = await CreateHandler(organizations, CreateLookups(), new FakeUnitOfWork())
            .Handle(new CreateExternalOrganizationCommand(
                "MOI", "جهة أخرى", OrganizationType.GovernmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.ExternalOrgCodeAlreadyExists.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Create_WithInvalidContact_ReturnsValidationError()
    {
        var result = await CreateHandler(
                new FakeExternalOrganizationRepository(), CreateLookups(), new FakeUnitOfWork())
            .Handle(new CreateExternalOrganizationCommand(
                "MOI", "وزارة الداخلية", OrganizationType.GovernmentId, ContactInfo: "not-a-contact"),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.ExternalOrgContactInvalid.Code, result.Error.Code);
    }

    [Fact]
    public async Task Create_WithUnknownOrganizationType_ReturnsValidationError()
    {
        var result = await CreateHandler(
                new FakeExternalOrganizationRepository(), CreateLookups(), new FakeUnitOfWork())
            .Handle(new CreateExternalOrganizationCommand(
                "MOI", "وزارة الداخلية", OrganizationTypeId: 99), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Lookup.InvalidId.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_WhenOrganizationIsMissing_ReturnsNotFound()
    {
        var result = await new UpdateExternalOrganizationCommandHandler(
                new FakeExternalOrganizationRepository(), CreateLookups(), new FakeUnitOfWork())
            .Handle(new UpdateExternalOrganizationCommand(
                Guid.NewGuid(), "اسم جديد", OrganizationType.GovernmentId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.ExternalOrgNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Update_WithValidPayload_ChangesDescriptiveFields()
    {
        var organizations = new FakeExternalOrganizationRepository();
        var organization = CreateOrganization();
        organizations.Add(organization);

        var result = await new UpdateExternalOrganizationCommandHandler(
                organizations, CreateLookups(), new FakeUnitOfWork())
            .Handle(new UpdateExternalOrganizationCommand(
                organization.ExternalOrganizationId,
                "وزارة الداخلية والسكان",
                OrganizationType.GovernmentId,
                "MOI & Population",
                "info@moi.gov.sy"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("وزارة الداخلية والسكان", result.Value.NameAr);
        Assert.Equal("info@moi.gov.sy", result.Value.ContactInfo);
        Assert.NotNull(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task Deactivate_ThenActivate_RoundTripsState()
    {
        var organizations = new FakeExternalOrganizationRepository();
        var organization = CreateOrganization();
        organizations.Add(organization);

        var deactivated = await new DeactivateExternalOrganizationCommandHandler(
                organizations, new FakeUnitOfWork())
            .Handle(new DeactivateExternalOrganizationCommand(organization.ExternalOrganizationId),
                CancellationToken.None);

        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);

        var activated = await new ActivateExternalOrganizationCommandHandler(
                organizations, new FakeUnitOfWork())
            .Handle(new ActivateExternalOrganizationCommand(organization.ExternalOrganizationId),
                CancellationToken.None);

        Assert.True(activated.IsSuccess);
        Assert.True(activated.Value.IsActive);
    }

    [Fact]
    public async Task List_ExcludesInactiveUnlessRequested()
    {
        var organizations = new FakeExternalOrganizationRepository();
        organizations.Add(CreateOrganization("MOI"));
        organizations.Add(CreateOrganization("BANK", active: false));

        var handler = new ListExternalOrganizationsQueryHandler(organizations);

        var active = await handler.Handle(
            new ListExternalOrganizationsQuery(), CancellationToken.None);
        Assert.True(active.IsSuccess);
        Assert.Single(active.Value);

        var all = await handler.Handle(
            new ListExternalOrganizationsQuery(IncludeInactive: true), CancellationToken.None);
        Assert.True(all.IsSuccess);
        Assert.Equal(2, all.Value.Count);
    }
}
