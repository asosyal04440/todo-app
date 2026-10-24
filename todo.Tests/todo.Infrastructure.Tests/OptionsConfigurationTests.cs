using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure;
using TodoApi.Infrastructure.Configuration;
using TodoApi.Infrastructure.Services.Email;

namespace todo.Infrastructure.Tests;

public class OptionsConfigurationTests
{
    [Fact]
    public void BindingPreservesDefaultsOptionalFeaturesAndApplicationSettings()
    {
        var values = ValidValues();
        values["Authentication:Google:ClientId"] = "   ";
        values["Authorization:AdminEmails:0"] = " admin@example.com ";
        using ServiceProvider provider = CreateProvider(values);
        provider.GetRequiredService<IStartupValidator>().Validate();

        var smtp = provider.GetRequiredService<IOptions<SmtpOptions>>().Value;
        Assert.Equal(587, smtp.Port);
        Assert.Equal(SmtpSecurity.StartTls, smtp.Security);
        Assert.False(provider.GetRequiredService<IEmailSender>().IsConfigured);
        Assert.False(provider.GetRequiredService<IOptions<GoogleAuthenticationOptions>>().Value.ToSettings().IsEnabled);
        Assert.Equal(10, provider.GetRequiredService<IOptions<SecurityOptions>>().Value.AuthenticationRequestsPerMinute);
        var reminders = provider.GetRequiredService<ReminderSettings>();
        Assert.True(reminders.IsEnabled);
        Assert.Equal(TimeSpan.FromHours(24), reminders.LeadTime);
        Assert.Equal(50, reminders.BatchSize);
        Assert.Equal(3, reminders.MaximumAttempts);
        Assert.Equal(TimeSpan.FromMinutes(15), reminders.RetryDelay);
        Assert.Equal(TimeSpan.FromDays(7), provider.GetRequiredService<RefreshTokenSettings>().Lifetime);
        Assert.Equal(UserRole.Admin, provider.GetRequiredService<IRoleAssignmentPolicy>().GetRoleForEmail("ADMIN@example.com"));
        Assert.Equal(UserRole.User, provider.GetRequiredService<IRoleAssignmentPolicy>().GetRoleForEmail("other@example.com"));
    }

    [Fact]
    public void ExplicitValuesAndSmtpFallbacksAreBoundWithoutChangingTheConfigNames()
    {
        var values = ValidValues();
        values["Jwt:AccessTokenLifetimeMinutes"] = "30";
        values["Jwt:RefreshTokenLifetimeDays"] = "14";
        values["Email:Smtp:Host"] = " smtp.example.com ";
        values["Email:Smtp:Security"] = "SslOnConnect";
        values["Email:Smtp:Port"] = "465";
        values["Email:Smtp:Username"] = " sender@example.com ";
        values["Email:Smtp:Password"] = " password-with-spaces ";
        values["Authentication:Google:ClientId"] = " client-id ";
        values["Reminders:Enabled"] = "false";
        values["Reminders:BatchSize"] = "12";
        values["Reminders:LeadTimeHours"] = "8";
        values["Security:AuthenticationRequestsPerMinute"] = "3";
        using ServiceProvider provider = CreateProvider(values);
        provider.GetRequiredService<IStartupValidator>().Validate();

        var jwt = provider.GetRequiredService<IOptions<JwtOptions>>().Value;
        Assert.Equal(30, jwt.AccessTokenLifetimeMinutes);
        Assert.Equal(TimeSpan.FromDays(14), provider.GetRequiredService<RefreshTokenSettings>().Lifetime);
        var smtp = provider.GetRequiredService<IOptions<SmtpOptions>>().Value.ToSettings();
        Assert.True(smtp.IsConfigured);
        Assert.Equal("smtp.example.com", smtp.Host);
        Assert.Equal(465, smtp.Port);
        Assert.Equal("sender@example.com", smtp.FromAddress);
        Assert.Equal(" password-with-spaces ", smtp.Password);
        Assert.Equal("Todo", smtp.FromName);
        Assert.Equal("client-id", provider.GetRequiredService<IOptions<GoogleAuthenticationOptions>>().Value.ToSettings().ClientId);
        var reminders = provider.GetRequiredService<ReminderSettings>();
        Assert.False(reminders.IsEnabled);
        Assert.Equal(12, reminders.BatchSize);
        Assert.Equal(TimeSpan.FromHours(8), reminders.LeadTime);
        Assert.Equal(3, provider.GetRequiredService<IOptions<SecurityOptions>>().Value.AuthenticationRequestsPerMinute);
    }

    [Fact]
    public void MissingSigningKeyFailsAtStartupWithoutIncludingASecretValue()
    {
        var values = ValidValues();
        values.Remove("Jwt:SigningKey");
        using ServiceProvider provider = CreateProvider(values);
        var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains("Jwt:SigningKey", error.Message);
    }

    [Theory]
    [InlineData("Jwt:Issuer", "")]
    [InlineData("Jwt:Audience", "")]
    [InlineData("Jwt:SigningKey", "too-short")]
    [InlineData("Jwt:AccessTokenLifetimeMinutes", "0")]
    [InlineData("Jwt:AccessTokenLifetimeMinutes", "-10")]
    [InlineData("Jwt:RefreshTokenLifetimeDays", "0")]
    [InlineData("Jwt:RefreshTokenLifetimeDays", "2147483647")]
    [InlineData("Email:Smtp:Port", "0")]
    [InlineData("Email:Smtp:Port", "65536")]
    [InlineData("Email:Smtp:Security", "99")]
    [InlineData("Reminders:LeadTimeHours", "0")]
    [InlineData("Reminders:BatchSize", "0")]
    [InlineData("Reminders:MaximumAttempts", "0")]
    [InlineData("Reminders:RetryDelayMinutes", "0")]
    [InlineData("Reminders:Schedule", " ")]
    [InlineData("Reminders:DisplayTimeZone", "Unknown/Zone")]
    [InlineData("Security:AuthenticationRequestsPerMinute", "0")]
    public void InvalidSemanticValuesFailAtStartupWithoutResolvingTheConsumer(string key, string value)
    {
        var values = ValidValues();
        values[key] = value;
        using ServiceProvider provider = CreateProvider(values);
        var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.NotEmpty(error.Failures);
        Assert.DoesNotContain(values["Jwt:SigningKey"]!, error.Message);
    }

    [Theory]
    [InlineData("Jwt:AccessTokenLifetimeMinutes", "not-a-number")]
    [InlineData("Jwt:RefreshTokenLifetimeDays", "1.5")]
    [InlineData("Email:Smtp:Port", "invalid")]
    [InlineData("Email:Smtp:Security", "invalid")]
    [InlineData("Reminders:Enabled", "invalid")]
    [InlineData("Reminders:BatchSize", "invalid")]
    [InlineData("Security:AuthenticationRequestsPerMinute", "invalid")]
    public void MalformedTypesAreRejectedByTheFrameworkBinderAtStartup(string key, string value)
    {
        var values = ValidValues();
        values[key] = value;
        using ServiceProvider provider = CreateProvider(values);
        var error = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains(key, error.Message);
    }

    [Fact]
    public void RemotePlaintextSmtpIsRejectedEvenWhenRemindersAreDisabled()
    {
        var values = ValidValues();
        values["Reminders:Enabled"] = "false";
        values["Email:Smtp:Host"] = "smtp.example.com";
        values["Email:Smtp:Security"] = "None";
        using ServiceProvider provider = CreateProvider(values);
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        ["ConnectionStrings:TodoDatabase"] = "Server=unused;Database=unused;Integrated Security=True",
        ["Jwt:Issuer"] = "OptionsTest",
        ["Jwt:Audience"] = "OptionsTest",
        ["Jwt:SigningKey"] = "options-test-key-long-enough-for-hmac-sha256",
        ["Jwt:AccessTokenLifetimeMinutes"] = "15",
        ["Jwt:RefreshTokenLifetimeDays"] = "7"
    };

    private static ServiceProvider CreateProvider(Dictionary<string, string?> values)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services.BuildServiceProvider();
    }
}
