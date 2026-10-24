namespace TodoApi.Infrastructure.Configuration;

public sealed class AuthorizationOptions
{
    public const string SectionName = "Authorization";
    public string[] AdminEmails { get; set; } = [];
}
