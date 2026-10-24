using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
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
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ITodoReadQueries, TodoReadQueries>();
        services.AddScoped<IUserReadQueries, UserReadQueries>();

        services.AddTodoOptions(configuration);
        services.AddSingleton<IRoleAssignmentPolicy>(provider => new ConfiguredRoleAssignmentPolicy(
            provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.AdminEmails));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<JwtOptions>>().Value.ToRefreshTokenSettings());
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<ReminderOptions>>().Value.ToSettings());
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();
        services.AddSingleton<IGoogleTokenValidator, GoogleIdTokenValidator>();
        services.AddSingleton<ISecureTokenGenerator, SecureTokenGenerator>();
        services.AddSingleton<IEmailSender, MailKitEmailSender>();

        return services;
    }
}
