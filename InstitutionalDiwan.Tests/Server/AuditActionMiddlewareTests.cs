using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using System.Text.Json;

namespace InstitutionalDiwan.Tests.Server;

public sealed class AuditActionMiddlewareTests
{
    private static readonly Guid ActorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DepartmentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public async Task Invoke_AuthenticatedApiAction_RecordsTheAction()
    {
        var recorder = new RecordingRecorder();
        var middleware = Middleware(recorder, context => SetStatusAsync(context, StatusCodes.Status201Created));
        var (context, userContext) = Request("POST", "/api/documents", "Documents", "CreateDraft", authenticated: true);

        await middleware.InvokeAsync(context, userContext);

        var record = Assert.Single(recorder.Records);
        Assert.Equal("CORRESPONDENCE", record.ModuleName);
        Assert.Equal(AuditActionType.CreateId, record.ActionTypeId);
        Assert.Equal(AuditEntityType.OperationId, record.EntityTypeId);
        Assert.Equal("Documents.CreateDraft", record.EntityId);
        Assert.Equal(ActorId, record.ActorUserId);
        Assert.Equal(DepartmentId, record.ActorDepartmentId);

        using var values = JsonDocument.Parse(record.NewValuesJson!);
        Assert.Equal("POST", values.RootElement.GetProperty("method").GetString());
        Assert.Equal("/api/documents", values.RootElement.GetProperty("path").GetString());
        Assert.Equal(StatusCodes.Status201Created, values.RootElement.GetProperty("statusCode").GetInt32());
        Assert.True(values.RootElement.GetProperty("succeeded").GetBoolean());
    }

    [Fact]
    public async Task Invoke_ReadAction_MapsToView()
    {
        var recorder = new RecordingRecorder();
        var middleware = Middleware(recorder, context => SetStatusAsync(context, StatusCodes.Status200OK));
        var (context, userContext) = Request("GET", "/api/documents", "Documents", "List", authenticated: true);

        await middleware.InvokeAsync(context, userContext);

        Assert.Equal(AuditActionType.ViewId, Assert.Single(recorder.Records).ActionTypeId);
    }

    [Fact]
    public async Task Invoke_AnonymousOrNonApiRequest_IsNotRecorded()
    {
        var recorder = new RecordingRecorder();
        var middleware = Middleware(recorder, _ => Task.CompletedTask);

        var (anonymous, anonymousUser) = Request(
            "POST", "/api/auth/login", "Auth", "Login", authenticated: false);
        await middleware.InvokeAsync(anonymous, anonymousUser);

        var (spa, spaUser) = Request("GET", "/documents", "Documents", "List", authenticated: true);
        await middleware.InvokeAsync(spa, spaUser);

        Assert.Empty(recorder.Records);
    }

    [Fact]
    public async Task Invoke_WhenTheActionThrows_RecordsAFailureAndRethrows()
    {
        var recorder = new RecordingRecorder();
        var middleware = Middleware(
            recorder,
            _ => throw new InvalidOperationException("boom"));
        var (context, userContext) = Request("POST", "/api/documents", "Documents", "CreateDraft", authenticated: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.InvokeAsync(context, userContext));

        using var values = JsonDocument.Parse(Assert.Single(recorder.Records).NewValuesJson!);
        Assert.Equal(StatusCodes.Status500InternalServerError, values.RootElement.GetProperty("statusCode").GetInt32());
        Assert.False(values.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal(nameof(InvalidOperationException), values.RootElement.GetProperty("error").GetString());
    }

    private static AuditActionMiddleware Middleware(
        RecordingRecorder recorder,
        RequestDelegate next) =>
        new(next, recorder, NullLogger<AuditActionMiddleware>.Instance);

    private static async Task SetStatusAsync(HttpContext context, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        await Task.CompletedTask;
    }

    private static (HttpContext Context, IUserContext UserContext) Request(
        string method,
        string path,
        string controller,
        string action,
        bool authenticated)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        httpContext.Request.Path = path;
        httpContext.Response.StatusCode = StatusCodes.Status200OK;

        var descriptor = new ControllerActionDescriptor
        {
            ControllerName = controller,
            ActionName = action
        };
        httpContext.SetEndpoint(new Endpoint(
            requestDelegate: null,
            new EndpointMetadataCollection(descriptor),
            $"{controller}.{action}"));

        if (authenticated)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, ActorId.ToString()),
                new Claim("dept_id", DepartmentId.ToString())
            }, authenticationType: "Test"));
        }

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        return (httpContext, new UserContext(accessor));
    }

    private sealed class RecordingRecorder : IAuditActionRecorder
    {
        public List<AuditActionRecord> Records { get; } = new();

        public Task RecordAsync(
            AuditActionRecord record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }
    }
}
