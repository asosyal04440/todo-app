namespace TodoApi.Application.Abstractions.Authentication;

public interface ICurrentUser
{
    int UserId { get; }

    bool IsAdmin { get; }
}
