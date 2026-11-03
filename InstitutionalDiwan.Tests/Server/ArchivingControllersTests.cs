using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// Guards the permission contract of Module 07: reading the archive needs <c>DOCUMENT_VIEW</c>, moving
/// a document in or out of it also needs <c>DOCUMENT_ARCHIVE</c>, and saved searches stay reachable to
/// any viewer because each answers only for its owner.
/// </summary>
public sealed class ArchivingControllersTests
{
    private static string[] PoliciesOf(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy!).ToArray();

    private static IEnumerable<MethodInfo> ActionsOf(Type controller) =>
        controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any());

    [Theory]
    [InlineData(typeof(DocumentArchiveController))]
    [InlineData(typeof(SavedSearchesController))]
    public void Controllers_RequireDocumentViewAtClassLevel(Type controller) =>
        Assert.Equal(new[] { Permissions.DocumentView }, PoliciesOf(controller));

    [Theory]
    [InlineData(nameof(DocumentArchiveController.Archive))]
    [InlineData(nameof(DocumentArchiveController.Unarchive))]
    public void ArchiveTransitions_RequireDocumentArchive(string action) =>
        Assert.Equal(
            new[] { Permissions.DocumentArchive },
            PoliciesOf(typeof(DocumentArchiveController).GetMethod(action)!));

    [Fact]
    public void ArchiveReads_AddNoFurtherPermission()
    {
        Assert.Empty(PoliciesOf(typeof(DocumentArchiveController).GetMethod(nameof(DocumentArchiveController.Search))!));
        Assert.Empty(PoliciesOf(typeof(DocumentArchiveController).GetMethod(nameof(DocumentArchiveController.ListEvents))!));
    }

    [Fact]
    public void SavedSearchActions_AddNoFurtherPermission() =>
        Assert.All(ActionsOf(typeof(SavedSearchesController)), action => Assert.Empty(PoliciesOf(action)));

    [Theory]
    [InlineData(typeof(DocumentArchiveController))]
    [InlineData(typeof(SavedSearchesController))]
    public void NoAction_AllowsAnonymousAccess(Type controller) =>
        Assert.All(ActionsOf(controller), action =>
            Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>()));

    [Theory]
    [InlineData(nameof(DocumentArchiveController.Search), "archive/documents")]
    [InlineData(nameof(DocumentArchiveController.ListEvents), "documents/{documentId:guid}/archive-events")]
    [InlineData(nameof(DocumentArchiveController.Archive), "documents/{documentId:guid}/archive")]
    [InlineData(nameof(DocumentArchiveController.Unarchive), "documents/{documentId:guid}/unarchive")]
    public void ArchiveRoutes_AreStable(string action, string template)
    {
        var attribute = typeof(DocumentArchiveController).GetMethod(action)!
            .GetCustomAttributes().OfType<HttpMethodAttribute>().Single();

        Assert.Equal(template, attribute.Template);
        Assert.Equal("api", typeof(DocumentArchiveController).GetCustomAttribute<RouteAttribute>()!.Template);
    }
}
