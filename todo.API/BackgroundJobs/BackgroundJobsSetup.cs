using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;

namespace TodoApi.API.BackgroundJobs;

public static class BackgroundJobsSetup
{
    public const string DashboardPath = "/hangfire";

    public static IServiceCollection AddTodoBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (AreRemindersEnabled(configuration) == false)
        {
            return services;
        }

        string connectionString = configuration.GetConnectionString("TodoDatabase")
            ?? throw new InvalidOperationException("The TodoDatabase connection string is missing.");

        services.AddHangfire(hangfire =>
        {
            hangfire.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            hangfire.UseSimpleAssemblyNameTypeSerializer();
            hangfire.UseRecommendedSerializerSettings();
            hangfire.UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                SqlClientFactory = Microsoft.Data.SqlClient.SqlClientFactory.Instance,
                PrepareSchemaIfNecessary = true
            });
        });

        services.AddHangfireServer(options =>
        {
            options.ServerName = "todo-api";
            options.WorkerCount = 2;
        });

        services.AddScoped<TodoReminderJob>();

        return services;
    }

    public static WebApplication UseTodoBackgroundJobs(this WebApplication app)
    {
        if (AreRemindersEnabled(app.Configuration) == false)
        {
            return app;
        }

        IRecurringJobManager recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        IEmailSender emailSender = app.Services.GetRequiredService<IEmailSender>();
        ReminderSettings reminderSettings = app.Services.GetRequiredService<ReminderSettings>();
        ILogger logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(BackgroundJobsSetup));

        if (emailSender.IsConfigured)
        {
            recurringJobs.AddOrUpdate<TodoReminderJob>(
                TodoReminderJob.RecurringJobId,
                job => job.RunAsync(CancellationToken.None),
                reminderSettings.Schedule);

            logger.LogInformation(
                "Todo reminders scheduled with '{Schedule}' for todos due within {LeadTime}.",
                reminderSettings.Schedule,
                reminderSettings.LeadTime);
        }
        else
        {
            recurringJobs.RemoveIfExists(TodoReminderJob.RecurringJobId);

            logger.LogWarning(
                "Todo reminders are not scheduled because SMTP is not configured. " +
                "Set Email:Smtp:Username and Email:Smtp:Password with 'dotnet user-secrets'.");
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapHangfireDashboard(DashboardPath, new DashboardOptions
            {
                DashboardTitle = "Todo Arka Plan İşleri",
                Authorization = new IDashboardAuthorizationFilter[] { new LocalRequestsOnlyAuthorizationFilter() }
            })
            .AllowAnonymous();
        }

        return app;
    }

    private static bool AreRemindersEnabled(IConfiguration configuration)
    {
        return configuration.GetValue("Reminders:Enabled", defaultValue: true);
    }
}
