using TodoApi.Infrastructure.Services.Email;

namespace TodoApi.Infrastructure.Configuration;

public sealed class SmtpOptions
{
    public const string SectionName = "Email:Smtp";
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public SmtpSecurity Security { get; set; } = SmtpSecurity.StartTls;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }

    public SmtpSettings ToSettings() => new(Host, Port, Security, Username, Password, FromAddress, FromName);
}
