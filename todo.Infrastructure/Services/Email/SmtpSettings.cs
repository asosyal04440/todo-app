using System;

namespace TodoApi.Infrastructure.Services.Email;

public enum SmtpSecurity
{
    StartTls = 1,

    SslOnConnect = 2,

    None = 3
}

public sealed class SmtpSettings
{
    public const string SectionName = "Email:Smtp";

    private readonly string _host;
    private readonly int _port;
    private readonly SmtpSecurity _security;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public SmtpSettings(
        string host,
        int port,
        SmtpSecurity security,
        string? username,
        string? password,
        string? fromAddress,
        string? fromName)
    {
        _host = host?.Trim() ?? string.Empty;
        _port = port;
        _security = security;
        _username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        _password = string.IsNullOrEmpty(password) ? null : password;

        _fromAddress = string.IsNullOrWhiteSpace(fromAddress) ? _username ?? string.Empty : fromAddress.Trim();
        _fromName = string.IsNullOrWhiteSpace(fromName) ? "Todo" : fromName.Trim();

        if (port <= 0 || port > 65535)
        {
            throw new InvalidOperationException("The Email:Smtp:Port setting must be a valid port number.");
        }

        if (security == SmtpSecurity.None && IsLocalHost(_host) == false && _host.Length > 0)
        {
            throw new InvalidOperationException(
                "Unencrypted SMTP is only allowed for a server on this machine. Use StartTls or SslOnConnect.");
        }
    }

    public string Host
    {
        get
        {
            return _host;
        }
    }

    public int Port
    {
        get
        {
            return _port;
        }
    }

    public SmtpSecurity Security
    {
        get
        {
            return _security;
        }
    }

    public string? Username
    {
        get
        {
            return _username;
        }
    }

    public string? Password
    {
        get
        {
            return _password;
        }
    }

    public string FromAddress
    {
        get
        {
            return _fromAddress;
        }
    }

    public string FromName
    {
        get
        {
            return _fromName;
        }
    }

    public bool IsConfigured
    {
        get
        {
            bool hasCredentialsIfNeeded = _username == null || _password != null;

            return _host.Length > 0 && _fromAddress.Length > 0 && hasCredentialsIfNeeded;
        }
    }

    private static bool IsLocalHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host == "127.0.0.1"
            || host == "::1";
    }
}
