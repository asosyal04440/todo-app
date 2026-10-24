using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.Common;

public sealed class PendingAuthSession
{
    internal PendingAuthSession(User user, RefreshToken token, string tokenValue)
    {
        User = user;
        Token = token;
        TokenValue = tokenValue;
    }

    internal User User { get; }

    internal RefreshToken Token { get; }

    internal string TokenValue { get; }
}
