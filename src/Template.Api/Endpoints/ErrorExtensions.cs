using Template.Domain.Results;

namespace Template.Api.Endpoints;

public static class ErrorExtensions
{
    /// <summary>
    /// Maps a domain error to an RFC 9457 problem details response.
    /// </summary>
    public static IResult ToProblem(this Error error) => error.Type switch
    {
        ErrorType.Validation => Results.ValidationProblem(error.ValidationErrors, title: error.Description),
        _ => Results.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: error.Type switch
            {
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            })
    };
}
