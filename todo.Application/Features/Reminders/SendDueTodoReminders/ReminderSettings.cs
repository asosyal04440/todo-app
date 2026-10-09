using System;

namespace TodoApi.Application.Features.Reminders.SendDueTodoReminders;

public sealed class ReminderSettings
{
    private readonly bool _isEnabled;
    private readonly TimeSpan _leadTime;
    private readonly string _schedule;
    private readonly int _batchSize;
    private readonly TimeZoneInfo _displayTimeZone;
    private readonly int _maximumAttempts;
    private readonly TimeSpan _retryDelay;

    public ReminderSettings(
        bool isEnabled,
        TimeSpan leadTime,
        string schedule,
        int batchSize,
        TimeZoneInfo displayTimeZone,
        int maximumAttempts,
        TimeSpan retryDelay)
    {
        if (leadTime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leadTime), "The reminder lead time must be positive.");
        }

        if (string.IsNullOrWhiteSpace(schedule))
        {
            throw new ArgumentException("A reminder schedule is required.", nameof(schedule));
        }

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), "The batch size must be positive.");
        }

        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttempts), "The maximum attempt count must be positive.");
        }

        if (retryDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(retryDelay), "The retry delay must be positive.");
        }

        _isEnabled = isEnabled;
        _leadTime = leadTime;
        _schedule = schedule.Trim();
        _batchSize = batchSize;
        _displayTimeZone = displayTimeZone ?? throw new ArgumentNullException(nameof(displayTimeZone));
        _maximumAttempts = maximumAttempts;
        _retryDelay = retryDelay;
    }

    public bool IsEnabled
    {
        get
        {
            return _isEnabled;
        }
    }

    public TimeSpan LeadTime
    {
        get
        {
            return _leadTime;
        }
    }

    public string Schedule
    {
        get
        {
            return _schedule;
        }
    }

    public int BatchSize
    {
        get
        {
            return _batchSize;
        }
    }

    public TimeZoneInfo DisplayTimeZone
    {
        get
        {
            return _displayTimeZone;
        }
    }

    public int MaximumAttempts
    {
        get
        {
            return _maximumAttempts;
        }
    }

    public TimeSpan RetryDelay
    {
        get
        {
            return _retryDelay;
        }
    }
}
