using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;
using TodoApi.Infrastructure.Persistence;
using TodoApi.Infrastructure.Services.Authentication;
using TodoApi.Infrastructure.Services.Email;

namespace TodoApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        string? connectionString = configuration.GetConnectionString("TodoDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The TodoDatabase connection string is missing.");
        }

        services.AddDbContext<TodoDbContext>(options =>
        {
            options.UseTodoSqlServer(connectionString);
        });

        services.AddScoped<ITodoListRepository, TodoListRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITodoReminderRepository, TodoReminderRepository>();

        JwtSettings jwtSettings = JwtSettings.FromConfiguration(configuration);
        GoogleAuthenticationSettings googleSettings =
            GoogleAuthenticationSettings.FromConfiguration(configuration);
        ConfiguredRoleAssignmentPolicy roleAssignmentPolicy =
            ConfiguredRoleAssignmentPolicy.FromConfiguration(configuration);
        RefreshTokenSettings refreshTokenSettings = ReadRefreshTokenSettings(configuration);
        SmtpSettings smtpSettings = SmtpSettings.FromConfiguration(configuration);
        ReminderSettings reminderSettings = ReadReminderSettings(configuration);

        services.AddSingleton(jwtSettings);
        services.AddSingleton(googleSettings);
        services.AddSingleton<IRoleAssignmentPolicy>(roleAssignmentPolicy);
        services.AddSingleton(refreshTokenSettings);
        services.AddSingleton(smtpSettings);
        services.AddSingleton(reminderSettings);
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IGoogleTokenValidator, GoogleIdTokenValidator>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        return services;
    }

    private static RefreshTokenSettings ReadRefreshTokenSettings(IConfiguration configuration)
    {
        string lifetimeText = configuration["Jwt:RefreshTokenLifetimeDays"] ?? string.Empty;
        bool lifetimeIsNumber = int.TryParse(lifetimeText, out int lifetimeDays);

        if (lifetimeIsNumber == false || lifetimeDays <= 0)
        {
            throw new InvalidOperationException(
                "The Jwt:RefreshTokenLifetimeDays setting must be a positive whole number.");
        }

        return new RefreshTokenSettings(TimeSpan.FromDays(lifetimeDays));
    }

    private static ReminderSettings ReadReminderSettings(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Reminders");

        bool isEnabled = ReadBoolean(section, "Enabled", defaultValue: true);
        int leadTimeHours = ReadPositiveInteger(section, "LeadTimeHours", defaultValue: 24);
        string schedule = section["Schedule"] ?? "*/5 * * * *";
        int batchSize = ReadPositiveInteger(section, "BatchSize", defaultValue: 50);
        string timeZoneId = section["DisplayTimeZone"] ?? "Europe/Istanbul";
        int maximumAttempts = ReadPositiveInteger(section, "MaximumAttempts", defaultValue: 3);
        int retryDelayMinutes = ReadPositiveInteger(section, "RetryDelayMinutes", defaultValue: 15);

        TimeZoneInfo displayTimeZone;

        try
        {
            displayTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"The Reminders:DisplayTimeZone setting '{timeZoneId}' is not a known time zone.",
                exception);
        }

        return new ReminderSettings(
            isEnabled,
            TimeSpan.FromHours(leadTimeHours),
            schedule,
            batchSize,
            displayTimeZone,
            maximumAttempts,
            TimeSpan.FromMinutes(retryDelayMinutes));
    }

    private static bool ReadBoolean(IConfigurationSection section, string key, bool defaultValue)
    {
        string? value = section[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out bool result) == false)
        {
            throw new InvalidOperationException($"The {section.Path}:{key} setting must be true or false.");
        }

        return result;
    }

    private static int ReadPositiveInteger(IConfigurationSection section, string key, int defaultValue)
    {
        string? value = section[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out int result) == false || result <= 0)
        {
            throw new InvalidOperationException($"The {section.Path}:{key} setting must be a positive whole number.");
        }

        return result;
    }
}
