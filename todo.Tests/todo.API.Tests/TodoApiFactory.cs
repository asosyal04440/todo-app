using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace todo.API.Tests;

public sealed class TodoApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "http://localhost:4200";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-that-is-long-enough");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.UseSetting("Reminders:Enabled", "false");
    }
}
