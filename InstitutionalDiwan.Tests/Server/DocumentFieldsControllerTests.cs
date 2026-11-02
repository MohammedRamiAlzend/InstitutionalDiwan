using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// Guards the permission contract of the dynamic-field endpoints: schema administration needs
/// <c>LOOKUP_MANAGE</c>, document-facing reads need <c>DOCUMENT_VIEW</c>, value edits <c>DOCUMENT_EDIT</c>.
/// </summary>
public sealed class DocumentFieldsControllerTests
{
    private static readonly string[] ExpectedAdministration =
    [
        nameof(DocumentFieldsController.ListFields),
        nameof(DocumentFieldsController.CreateField),
        nameof(DocumentFieldsController.ListVersions),
        nameof(DocumentFieldsController.CreateDraft),
        nameof(DocumentFieldsController.GetVersion),
        nameof(DocumentFieldsController.DiscardDraft),
        nameof(DocumentFieldsController.AddFieldToDraft),
        nameof(DocumentFieldsController.UpdateDraftField),
        nameof(DocumentFieldsController.RemoveDraftField),
        nameof(DocumentFieldsController.Publish)
    ];

    private static string? PolicyOf(string action) =>
        typeof(DocumentFieldsController).GetMethod(action)!
            .GetCustomAttribute<AuthorizeAttribute>()?.Policy;

    [Fact]
    public void SchemaAdministration_RequiresLookupManage()
    {
        foreach (var action in ExpectedAdministration)
            Assert.Equal(Permissions.LookupManage, PolicyOf(action));
    }

    [Fact]
    public void DocumentFacingEndpoints_UseDocumentPermissions()
    {
        Assert.Equal(Permissions.DocumentView, PolicyOf(nameof(DocumentFieldsController.GetPublishedSchema)));
        Assert.Equal(Permissions.DocumentView, PolicyOf(nameof(DocumentFieldsController.GetDocumentFields)));
        Assert.Equal(Permissions.DocumentEdit, PolicyOf(nameof(DocumentFieldsController.SetDocumentFields)));
    }

    [Fact]
    public void EveryAction_DeclaresAnExplicitPolicy()
    {
        var actions = typeof(DocumentFieldsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());

        Assert.All(actions, method => Assert.False(
            string.IsNullOrEmpty(method.GetCustomAttribute<AuthorizeAttribute>()?.Policy),
            $"{method.Name} must declare its own permission policy."));
    }
}
