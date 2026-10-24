using TodoApi.Application.Features.Reminders.SendDueTodoReminders;

namespace TodoApi.Infrastructure.Configuration;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";
    public bool Enabled { get; set; } = true;
    public int LeadTimeHours { get; set; } = 24;
    public string Schedule { get; set; } = "*/5 * * * *";
    public int BatchSize { get; set; } = 50;
    public string DisplayTimeZone { get; set; } = "Europe/Istanbul";
    public int MaximumAttempts { get; set; } = 3;
    public int RetryDelayMinutes { get; set; } = 15;

    public ReminderSettings ToSettings() => new(Enabled, TimeSpan.FromHours(LeadTimeHours), Schedule,
        BatchSize, TimeZoneInfo.FindSystemTimeZoneById(DisplayTimeZone), MaximumAttempts,
        TimeSpan.FromMinutes(RetryDelayMinutes));
}
