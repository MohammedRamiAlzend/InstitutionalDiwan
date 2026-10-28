using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Server.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Restricts forced-change sessions before any protected endpoint executes, including routes that
/// only use [Authorize] and do not require a permission policy. Run after authentication/routing.
/// </summary>
public sealed class ForcedPasswordChangeMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null ||
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null ||
            context.User.Identity?.IsAuthenticated != true ||
            !context.User.HasClaim("must_change_password", "true") ||
            IsPasswordRecoveryEndpoint(context, endpoint))
        {
            await next(context);
            return;
        }

        var error = DomainErrors.Identity.PasswordChangeRequired;
        await Results.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: error.Code,
            detail: error.Description).ExecuteAsync(context);
    }

    private static bool IsPasswordRecoveryEndpoint(HttpContext context, Endpoint endpoint)
    {
        // Match the selected action, not a path prefix that could exempt unrelated endpoints.
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        if (action?.ControllerTypeInfo.AsType() != typeof(AuthController))
            return false;

        return (HttpMethods.IsGet(context.Request.Method) &&
                action.MethodInfo.Name == nameof(AuthController.Me)) ||
               (HttpMethods.IsPost(context.Request.Method) &&
                action.MethodInfo.Name == nameof(AuthController.ChangePassword));
    }
}
