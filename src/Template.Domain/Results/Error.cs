using System.Collections.ObjectModel;

namespace Template.Domain.Results;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized
}

public sealed record Error(string Code, string Description, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; } =
        ReadOnlyDictionary<string, string[]>.Empty;

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);

    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation)
        {
            ValidationErrors = errors
        };
}
