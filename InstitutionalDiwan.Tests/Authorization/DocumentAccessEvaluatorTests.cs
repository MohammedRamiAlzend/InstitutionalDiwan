using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Authorization;

public sealed class DocumentAccessEvaluatorTests
{
    private static Document CreateDocument(
        Guid owningDepartmentId,
        Guid createdByUserId,
        int confidentialityLevelId = DocumentConfidentialityLevel.NormalId)
    {
        var document = Document.CreateDraft(
            "طلب إحاطة",
            DocumentType.InternalMemoId,
            DocumentDirection.InternalId,
            owningDepartmentId,
            createdByUserId,
            confidentialityLevelId: confidentialityLevelId);

        Assert.True(document.IsSuccess);
        return document.Value;
    }

    private static (DocumentAccessEvaluator Evaluator,
        FakeUserContext Context,
        FakeDocumentRepository Documents,
        FakeDocumentAccessGrantRepository Grants,
        FakeDepartmentScopeEvaluator Scope) CreateEvaluator()
    {
        var context = new FakeUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var documents = new FakeDocumentRepository();
        var grants = new FakeDocumentAccessGrantRepository();
        var scope = new FakeDepartmentScopeEvaluator();

        var evaluator = new DocumentAccessEvaluator(context, documents, grants, scope, TestClock.Instance);
        return (evaluator, context, documents, grants, scope);
    }

    [Fact]
    public async Task CanAccessDocument_WhenAuthor_ReturnsTrue()
    {
        var (evaluator, context, documents, _, _) = CreateEvaluator();
        var document = CreateDocument(Guid.NewGuid(), context.UserId);
        documents.Add(document);

        Assert.True(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ManageId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenGrantCoversLevel_ReturnsTrue()
    {
        var (evaluator, context, documents, grants, _) = CreateEvaluator();
        var document = CreateDocument(Guid.NewGuid(), Guid.NewGuid());
        documents.Add(document);

        var grant = DocumentAccessGrant.Grant(
            document.DocumentId, context.UserId, AccessLevel.DownloadId, Guid.NewGuid());
        Assert.True(grant.IsSuccess);
        grants.Add(grant.Value);

        Assert.True(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId));
        Assert.True(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.DownloadId));
        Assert.False(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ManageId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenGrantIsRevoked_IsIgnored()
    {
        var (evaluator, context, documents, grants, _) = CreateEvaluator();
        var document = CreateDocument(Guid.NewGuid(), Guid.NewGuid());
        documents.Add(document);

        var grant = DocumentAccessGrant.Grant(
            document.DocumentId, context.UserId, AccessLevel.ManageId, Guid.NewGuid());
        Assert.True(grant.IsSuccess);
        grant.Value.Revoke(Guid.NewGuid());
        grants.Add(grant.Value);

        Assert.False(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenInScope_AllowsViewAndDownload()
    {
        var (evaluator, context, documents, _, scope) = CreateEvaluator();
        var departmentId = Guid.NewGuid();
        var document = CreateDocument(departmentId, Guid.NewGuid());
        documents.Add(document);
        scope.AccessibleDepartments.Add(departmentId);

        Assert.True(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.DownloadId));
        Assert.False(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ForwardId));
        Assert.False(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ManageId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenInstitutionWideAndInScope_AllowsForward()
    {
        var (evaluator, context, documents, _, scope) = CreateEvaluator();
        var departmentId = Guid.NewGuid();
        var document = CreateDocument(departmentId, Guid.NewGuid());
        documents.Add(document);
        scope.AccessibleDepartments.Add(departmentId);
        context.ScopeTypeId = UserScopeType.InstitutionId;

        Assert.True(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ForwardId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenConfidentialAndScopeOnly_ReturnsFalse()
    {
        var (evaluator, _, documents, _, scope) = CreateEvaluator();
        var departmentId = Guid.NewGuid();
        var document = CreateDocument(
            departmentId, Guid.NewGuid(), DocumentConfidentialityLevel.TopSecretId);
        documents.Add(document);
        scope.AccessibleDepartments.Add(departmentId);

        Assert.False(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenConfidentialButGranted_ReturnsTrue()
    {
        var (evaluator, context, documents, grants, _) = CreateEvaluator();
        var document = CreateDocument(
            Guid.NewGuid(), Guid.NewGuid(), DocumentConfidentialityLevel.SecretId);
        documents.Add(document);

        var grant = DocumentAccessGrant.Grant(
            document.DocumentId, context.UserId, AccessLevel.ViewId, Guid.NewGuid());
        Assert.True(grant.IsSuccess);
        grants.Add(grant.Value);

        Assert.True(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenDocumentIsMissing_ReturnsFalse()
    {
        var (evaluator, _, _, _, _) = CreateEvaluator();

        Assert.False(await evaluator.CanAccessDocumentAsync(Guid.NewGuid(), AccessLevel.ViewId));
    }

    [Fact]
    public async Task CanAccessDocument_WhenAnonymous_ReturnsFalse()
    {
        var (evaluator, context, documents, _, _) = CreateEvaluator();
        var document = CreateDocument(Guid.NewGuid(), Guid.NewGuid());
        documents.Add(document);
        context.IsAuthenticated = false;

        Assert.False(await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId));
    }
}
