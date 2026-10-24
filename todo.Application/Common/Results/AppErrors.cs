namespace TodoApi.Application.Common.Results;

public static class AppErrors
{
    public static AppError ListNotFound => new(ErrorKind.NotFound, ErrorCode.ListNotFound, "The todo list was not found.");
    public static AppError TodoNotFound => new(ErrorKind.NotFound, ErrorCode.TodoNotFound, "The todo was not found.");
    public static AppError AccessDenied => new(ErrorKind.Forbidden, ErrorCode.AccessDenied, "The user cannot access this todo list.");
    public static AppError CurrentUserMissing => new(ErrorKind.Unauthorized, ErrorCode.CurrentUserMissing, "The signed-in user was not found.");
    public static AppError InvalidCredentials => new(ErrorKind.Unauthorized, ErrorCode.InvalidCredentials, "The email address or password is incorrect.");
    public static AppError InvalidSession => new(ErrorKind.Unauthorized, ErrorCode.InvalidSession, "The session has expired. Please sign in again.");
    public static AppError GoogleTokenInvalid => new(ErrorKind.Unauthorized, ErrorCode.GoogleTokenInvalid, "The Google sign-in token is not valid.");
    public static AppError GoogleEmailUnverified => new(ErrorKind.Unauthorized, ErrorCode.GoogleEmailUnverified, "The Google account email address has not been verified.");
    public static AppError EmailAlreadyRegistered => new(ErrorKind.Conflict, ErrorCode.EmailAlreadyRegistered, "The email address is already registered.");
    public static AppError ConcurrentWrite => new(ErrorKind.Conflict, ErrorCode.ConcurrentWrite, "The record was changed or deleted by another request. Reload and try again.");
    public static AppError DuplicateName(string message) => new(ErrorKind.Conflict, ErrorCode.DuplicateName, message);
    public static AppError DataConflict(string message) => new(ErrorKind.Conflict, ErrorCode.DataConflict, message);
    public static AppError DomainRule(string message) => new(ErrorKind.Validation, ErrorCode.DomainRuleViolation, message);
    public static AppError Authentication(string message) => new(ErrorKind.Unauthorized, ErrorCode.AuthenticationFailed, message);
    public static AppError Validation(IReadOnlyDictionary<string, string[]> errors) =>
        new(ErrorKind.Validation, ErrorCode.ValidationFailed,
            errors.Values.SelectMany(messages => messages).FirstOrDefault() ?? "One or more validation errors occurred.", errors);
}
