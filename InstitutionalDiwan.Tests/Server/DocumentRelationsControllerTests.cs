using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// Guards the permission contract of Module 08: reading links needs <c>DOCUMENT_VIEW</c>, and creating
/// or removing one also needs <c>DOCUMENT_EDIT</c>.
/// </summary>
public sealed class DocumentRelationsControllerTests
{
    private static readonly Type Controller = typeof(DocumentRelationsController);

    private static string[] PoliciesOf(MemberInfo member) =>
        member.GetCustomAttributes<AuthorizeAttribute>().Select(attribute => attribute.Policy!).ToArray();

    [Fact]
    public void Controller_RequiresDocumentViewAtClassLevel() =>
        Assert.Equal(new[] { Permissions.DocumentView }, PoliciesOf(Controller));

    [Theory]
    [InlineData(nameof(DocumentRelationsController.Create))]
    [InlineData(nameof(DocumentRelationsController.Remove))]
    public void Writes_RequireDocumentEdit(string action) =>
        Assert.Equal(new[] { Permissions.DocumentEdit }, PoliciesOf(Controller.GetMethod(action)!));

    [Theory]
    [InlineData(nameof(DocumentRelationsController.List))]
    [InlineData(nameof(DocumentRelationsController.Graph))]
    [InlineData(nameof(DocumentRelationsController.ListEvents))]
    public void Reads_AddNoFurtherPermission(string action) =>
        Assert.Empty(PoliciesOf(Controller.GetMethod(action)!));

    [Fact]
    public void NoAction_AllowsAnonymousAccess() =>
        Assert.All(
            Controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes().OfType<HttpMethodAttribute>().Any()),
            action => Assert.Null(action.GetCustomAttribute<AllowAnonymousAttribute>()));

    [Theory]
    [InlineData(nameof(DocumentRelationsController.List), null)]
    [InlineData(nameof(DocumentRelationsController.Graph), "graph")]
    [InlineData(nameof(DocumentRelationsController.ListEvents), "events")]
    [InlineData(nameof(DocumentRelationsController.Create), null)]
    [InlineData(nameof(DocumentRelationsController.Remove), "{relationId:guid}")]
    public void Routes_AreStable(string action, string? template)
    {
        var attribute = Controller.GetMethod(action)!.GetCustomAttributes().OfType<HttpMethodAttribute>().Single();

        Assert.Equal(template, attribute.Template);
        Assert.Equal(
            "api/documents/{documentId:guid}/relations",
            Controller.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>()!.Template);
    }
}
