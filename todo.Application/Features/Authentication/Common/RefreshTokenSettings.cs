using System;

namespace TodoApi.Application.Features.Authentication.Common;

public sealed class RefreshTokenSettings
{
    private readonly TimeSpan _lifetime;

    public RefreshTokenSettings(TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime),
                "A refresh token lifetime must be positive.");
        }

        _lifetime = lifetime;
    }

    public TimeSpan Lifetime
    {
        get
        {
            return _lifetime;
        }
    }
}
