namespace TodoApi.Infrastructure.Configuration;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";
    public int AuthenticationRequestsPerMinute { get; set; } = 10;
}
