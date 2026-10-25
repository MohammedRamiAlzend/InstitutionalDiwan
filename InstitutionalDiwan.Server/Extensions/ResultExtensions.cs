using InstitutionalDiwan.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Extensions;

/// <summary>
/// Translates the domain <see cref="Result"/> / <see cref="Result{TValue}"/> into HTTP responses
/// (RFC 7807 <see cref="ProblemDetails"/> on failure). This is the single place where domain error
/// types become HTTP status codes.
/// </summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new OkResult() : ToProblem(result.Error);

    public static IActionResult ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToProblem(result.Error);

    /// <summary>
    /// Translates a failed <see cref="Error"/> on its own. Used by endpoints that return a payload
    /// which cannot go through <see cref="OkObjectResult"/> — for example a file stream.
    /// </summary>
    public static IActionResult ToActionResult(this Error error) => ToProblem(error);

    private static IActionResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Description,
            Type = $"https://httpstatuses.io/{statusCode}"
        })
        {
            StatusCode = statusCode
        };
    }
}
