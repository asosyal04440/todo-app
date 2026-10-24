using System.Collections.ObjectModel;

namespace TodoApi.Application.Common.Results;

public enum ErrorKind { Validation, NotFound, Forbidden, Unauthorized, Conflict }
public enum ErrorCode
{
    ValidationFailed, DomainRuleViolation, ListNotFound, TodoNotFound, AccessDenied,
    CurrentUserMissing, InvalidCredentials, InvalidSession, GoogleTokenInvalid,
    GoogleEmailUnverified, AuthenticationFailed, DuplicateName, EmailAlreadyRegistered,
    DataConflict, ConcurrentWrite
}

public sealed class AppError
{
    internal AppError(ErrorKind kind, ErrorCode code, string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        Kind = kind;
        Code = code;
        Message = message;
        Errors = new ReadOnlyDictionary<string, string[]>((errors ?? new Dictionary<string, string[]>())
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
    }

    public ErrorKind Kind { get; }
    public ErrorCode Code { get; }
    public string Message { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
