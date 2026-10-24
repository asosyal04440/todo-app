namespace TodoApi.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    int UserId { get; }

    int? UserIdOrNull => UserId > 0 ? UserId : null;

    bool IsAdmin { get; }
}
