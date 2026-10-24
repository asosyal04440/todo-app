using System;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class GoogleAuthenticationSettings
{
    public const string SectionName = "Authentication:Google";

    private readonly string? _clientId;

    public GoogleAuthenticationSettings(string? clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _clientId = null;
        }
        else
        {
            _clientId = clientId.Trim();
        }
    }

    public string? ClientId
    {
        get
        {
            return _clientId;
        }
    }

    public bool IsEnabled
    {
        get
        {
            return _clientId != null;
        }
    }
}
