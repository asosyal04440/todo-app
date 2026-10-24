using TodoApi.Domain.Enums;
using TodoApi.Domain.Common;
using TodoApi.Domain.Exceptions;
using TodoApi.Domain.Specifications;

namespace TodoApi.Domain.Entities;

public class TodoItem : AuditableEntity
{
    public const int MaximumTitleLength = 120;
    public const int MaximumNormalizedTitleLength = MaximumTitleLength * 3;
    public const int MaximumDescriptionLength = 1000;

    private int _id;
    private int _todoListId;

    private string _title = string.Empty;
    private string? _description;
    private bool _isCompleted;
    private DateTime? _dueAtUtc;
    private TodoPriority _priority = TodoPriority.Normal;
    private DateTime? _reminderSentAtUtc;
    private int _reminderAttemptCount;
    private DateTime? _reminderLastAttemptAtUtc;

    internal TodoItem(
        int todoListId,
        string title,
        string? description,
        DateTime? dueAtUtc,
        TodoPriority priority)
    {
        if (todoListId < 0)
        {
            throw new DomainRuleViolationException(
                "A todo list id cannot be negative.");
        }

        TodoListId = todoListId;

        string normalizedTitle = ValidateAndNormalizeTitle(title);
        Title = normalizedTitle;
        NormalizedTitle = NameComparison.NormalizeForLookup(normalizedTitle);

        string? normalizedDescription = ValidateAndNormalizeDescription(description);
        Description = normalizedDescription;

        DateTime? validatedDueAtUtc = ValidateDueAtUtc(dueAtUtc);
        DueAtUtc = validatedDueAtUtc;

        TodoPriority validatedPriority = ValidatePriority(priority);
        Priority = validatedPriority;

        IsCompleted = false;

    }

    public int Id
    {
        get
        {
            return _id;
        }
        private set
        {
            _id = value;
        }
    }

    public string NormalizedTitle { get; private set; }

    public int TodoListId
    {
        get
        {
            return _todoListId;
        }
        private set
        {
            _todoListId = value;
        }
    }

    public string Title
    {
        get
        {
            return _title;
        }
        private set
        {
            _title = value;
        }
    }

    public string? Description
    {
        get
        {
            return _description;
        }
        private set
        {
            _description = value;
        }
    }

    public bool IsCompleted
    {
        get
        {
            return _isCompleted;
        }
        private set
        {
            _isCompleted = value;
        }
    }

    public DateTime? DueAtUtc
    {
        get
        {
            return _dueAtUtc;
        }
        private set
        {
            _dueAtUtc = value;
        }
    }

    public TodoPriority Priority
    {
        get
        {
            return _priority;
        }
        private set
        {
            _priority = value;
        }
    }

    public DateTime? ReminderSentAtUtc
    {
        get
        {
            return _reminderSentAtUtc;
        }
        private set
        {
            _reminderSentAtUtc = value;
        }
    }

    public int ReminderAttemptCount
    {
        get
        {
            return _reminderAttemptCount;
        }
        private set
        {
            _reminderAttemptCount = value;
        }
    }

    public DateTime? ReminderLastAttemptAtUtc
    {
        get
        {
            return _reminderLastAttemptAtUtc;
        }
        private set
        {
            _reminderLastAttemptAtUtc = value;
        }
    }

    internal void Rename(string title)
    {
        string normalizedTitle = ValidateAndNormalizeTitle(title);
        string titleKey = NameComparison.NormalizeForLookup(normalizedTitle);
        Title = normalizedTitle;
        NormalizedTitle = titleKey;
    }

    public void ChangeDescription(string? description)
    {
        string? normalizedDescription = ValidateAndNormalizeDescription(description);
        Description = normalizedDescription;
    }

    public void SetDueAtUtc(DateTime? dueAtUtc)
    {
        DateTime? validatedDueAtUtc = ValidateDueAtUtc(dueAtUtc);

        if (validatedDueAtUtc != DueAtUtc)
        {
            ReminderSentAtUtc = null;
            ReminderAttemptCount = 0;
            ReminderLastAttemptAtUtc = null;
        }

        DueAtUtc = validatedDueAtUtc;
    }

    public bool NeedsReminder(DateTime nowUtc, TimeSpan reminderLeadTime)
    {
        return TodoReminderSpecification.NeedsReminder(this, nowUtc, reminderLeadTime);
    }

    public void MarkReminderSent(DateTime nowUtc)
    {
        ReminderSentAtUtc = nowUtc;
    }

    public void RecordFailedReminderAttempt(DateTime nowUtc)
    {
        ReminderAttemptCount++;
        ReminderLastAttemptAtUtc = nowUtc;
    }

    public bool CanRetryReminder(DateTime nowUtc, int maximumAttempts, TimeSpan retryDelay)
    {
        return TodoReminderSpecification.CanRetryReminder(this, nowUtc, maximumAttempts, retryDelay);
    }

    public void ChangePriority(TodoPriority priority)
    {
        TodoPriority validatedPriority = ValidatePriority(priority);
        Priority = validatedPriority;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }

    public void MarkIncomplete()
    {
        IsCompleted = false;
    }

    private static string ValidateAndNormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainRuleViolationException(
                "A todo title cannot be empty.");
        }

        string normalizedTitle = title.Trim();

        if (normalizedTitle.Length > MaximumTitleLength)
        {
            throw new DomainRuleViolationException(
                $"A todo title cannot exceed {MaximumTitleLength} characters.");
        }

        return normalizedTitle;
    }

    private static string? ValidateAndNormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        string normalizedDescription = description.Trim();

        if (normalizedDescription.Length > MaximumDescriptionLength)
        {
            throw new DomainRuleViolationException(
                $"A todo description cannot exceed {MaximumDescriptionLength} characters.");
        }

        return normalizedDescription;
    }

    private static DateTime? ValidateDueAtUtc(DateTime? dueAtUtc)
    {
        if (dueAtUtc.HasValue)
        {
            DateTime providedDueAtUtc = dueAtUtc.Value;

            if (providedDueAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new DomainRuleViolationException(
                    "A todo due date must be expressed in UTC.");
            }
        }

        return dueAtUtc;
    }

    private static TodoPriority ValidatePriority(TodoPriority priority)
    {
        bool isDefinedPriority = Enum.IsDefined(typeof(TodoPriority), priority);

        if (isDefinedPriority == false)
        {
            throw new DomainRuleViolationException(
                "The selected todo priority is not supported.");
        }

        return priority;
    }
}
