using TodoApi.Infrastructure.Services.Authentication;

namespace TodoApi.Infrastructure.Configuration;

public sealed class GoogleAuthenticationOptions
{
    public const string SectionName = "Authentication:Google";
    public string? ClientId { get; set; }
    public GoogleAuthenticationSettings ToSettings() => new(ClientId);
}
