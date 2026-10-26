using InstitutionalDiwan.Application.Abstractions.Audit;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Entities.Audit;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Diagnostics;
using System.Text.Json;

namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Appends one action-level audit entry for every authenticated <c>/api</c> controller action,
/// whatever its outcome. Entity-level rows describe what changed; these rows describe what the
/// caller did, including reads and requests that returned an error.
/// </summary>
public sealed class AuditActionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private const int EntityIdMaxLength = 100;

    private readonly RequestDelegate _next;
    private readonly IAuditActionRecorder _recorder;
    private readonly ILogger<AuditActionMiddleware> _logger;

    public AuditActionMiddleware(
        RequestDelegate next,
        IAuditActionRecorder recorder,
        ILogger<AuditActionMiddleware> logger)
    {
        _next = next;
        _recorder = recorder;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();

        if (descriptor is null ||
            !context.Request.Path.StartsWithSegments("/api") ||
            HttpMethods.IsOptions(context.Request.Method) ||
            !userContext.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? failure = null;

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            await RecordAsync(context, descriptor, userContext, failure, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task RecordAsync(
        HttpContext context,
        ControllerActionDescriptor descriptor,
        IUserContext userContext,
        Exception? failure,
        long durationMilliseconds)
    {
        try
        {
            var statusCode = failure is null
                ? context.Response.StatusCode
                : StatusCodes.Status500InternalServerError;

            var newValues = JsonSerializer.Serialize(new
            {
                method = context.Request.Method,
                path = context.Request.Path.Value,
                statusCode,
                succeeded = failure is null && statusCode < StatusCodes.Status400BadRequest,
                durationMs = durationMilliseconds,
                error = failure?.GetType().Name
            }, JsonOptions);

            var entityId = $"{descriptor.ControllerName}.{descriptor.ActionName}";
            if (entityId.Length > EntityIdMaxLength)
                entityId = entityId[..EntityIdMaxLength];

            await _recorder.RecordAsync(
                new AuditActionRecord(
                    ResolveModule(descriptor.ControllerName),
                    ResolveActionTypeId(descriptor.ActionName, context.Request.Method),
                    AuditEntityType.OperationId,
                    entityId,
                    userContext.UserId == Guid.Empty ? null : userContext.UserId,
                    userContext.PrimaryDepartmentId == Guid.Empty ? null : userContext.PrimaryDepartmentId,
                    newValues),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            // Recording is best-effort: it must never replace the response the action produced.
            _logger.LogError(
                exception,
                "Could not record the audit action for {Controller}.{Action}.",
                descriptor.ControllerName,
                descriptor.ActionName);
        }
    }

    private static string ResolveModule(string controllerName) => controllerName switch
    {
        "Auth" or "Users" or "Roles" or "Permissions" or "Employees" or "DocumentAccessGrants"
            => "IDENTITY",
        "Documents" => "CORRESPONDENCE",
        "DocumentFiles" => "ATTACHMENTS",
        "Movements" => "MOVEMENTS",
        "NumberingRegistries" or "DocumentNumbers" or "DepartmentRegisters" => "NUMBERING",
        "Departments" or "InstitutionSettings" or "ExternalOrganizations" => "ORGANIZATION",
        "AuditLogs" => "AUDIT",
        _ => controllerName.ToUpperInvariant()
    };

    private static int ResolveActionTypeId(string actionName, string method)
    {
        if (Contains(actionName, "Logout"))
            return AuditActionType.LogoutId;

        if (Contains(actionName, "Login"))
            return AuditActionType.LoginId;

        if (Contains(actionName, "Approve"))
            return AuditActionType.ApproveId;

        if (StartsWithAny(actionName, "Create", "Add", "Register", "Issue", "Grant", "Upload"))
            return AuditActionType.CreateId;

        if (StartsWithAny(actionName, "Delete", "Remove", "Revoke"))
            return AuditActionType.DeleteId;

        if (StartsWithAny(
                actionName,
                "List", "Get", "Search", "Tree", "Inbox", "Outbox", "Download", "Verify", "Me"))
        {
            return AuditActionType.ViewId;
        }

        if (StartsWithAny(
                actionName,
                "Update", "Change", "Move", "Assign", "Set", "Activate", "Deactivate", "End",
                "Send", "Receive", "Complete", "Redirect", "Close", "Return", "Reject", "Cancel",
                "Archive", "Relabel", "Record"))
        {
            return AuditActionType.UpdateId;
        }

        if (StartsWithAny(actionName, "Run"))
            return AuditActionType.ExecuteId;

        return method switch
        {
            _ when HttpMethods.IsGet(method) || HttpMethods.IsHead(method) => AuditActionType.ViewId,
            _ when HttpMethods.IsDelete(method) => AuditActionType.DeleteId,
            _ when HttpMethods.IsPost(method) => AuditActionType.CreateId,
            _ => AuditActionType.ExecuteId
        };
    }

    private static bool Contains(string value, string fragment) =>
        value.Contains(fragment, StringComparison.OrdinalIgnoreCase);

    private static bool StartsWithAny(string value, params string[] prefixes) =>
        prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
