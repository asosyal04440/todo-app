using TodoApi.Application.Abstractions.Authentication;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeCurrentUser : ICurrentUser
{
    public const int ListOwnerUserId = 1;
    public const int OtherUserId = 2;
    public const int AdminUserId = 99;

    public FakeCurrentUser(int userId, bool isAdmin)
    {
        UserId = userId;
        IsAdmin = isAdmin;
    }

    public int UserId { get; }

    public bool IsAdmin { get; }

    public static FakeCurrentUser ListOwner()
    {
        return new FakeCurrentUser(ListOwnerUserId, isAdmin: false);
    }

    public static FakeCurrentUser OtherUser()
    {
        return new FakeCurrentUser(OtherUserId, isAdmin: false);
    }

    public static FakeCurrentUser Admin()
    {
        return new FakeCurrentUser(AdminUserId, isAdmin: true);
    }
}
