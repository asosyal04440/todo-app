using FluentValidation;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Common.Validation;

public static class SharedValidationRules
{
    public const int TodoDueDateMaximumYearsAhead = 10;
    public const string TodoDueDateInThePastMessage = "'Bitiş tarihi' geçmişte olamaz.";
    public const string TodoDueDateTooFarAheadMessage = "'Bitiş tarihi' en fazla 10 yıl sonrası olabilir.";

    public static readonly TimeSpan TodoDueDateClockTolerance = TimeSpan.FromMinutes(5);

    public static IRuleBuilderOptions<T, string> TodoListName<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty()
            .MaximumLength(TodoList.MaximumNameLength)
            .WithName("Liste adı");
    }

    public static IRuleBuilderOptions<T, string> TodoTitle<T>(this IRuleBuilder<T, string> rule)
    {
        return rule
            .NotEmpty()
            .MaximumLength(TodoItem.MaximumTitleLength)
            .WithName("Görev adı");
    }

    public static IRuleBuilderOptions<T, string?> TodoDescription<T>(this IRuleBuilder<T, string?> rule)
    {
        return rule
            .MaximumLength(TodoItem.MaximumDescriptionLength)
            .WithName("Açıklama");
    }

    public static IRuleBuilderOptions<T, TodoPriority> TodoPriority<T>(this IRuleBuilder<T, TodoPriority> rule)
    {
        return rule
            .IsInEnum()
            .WithName("Öncelik");
    }

    public static IRuleBuilderOptions<T, DateTime?> TodoDueDate<T>(
        this IRuleBuilder<T, DateTime?> rule,
        TimeProvider timeProvider)
    {
        return rule
            .Must(dueAtUtc => dueAtUtc == null
                || IsTodoDueDateTooFarAhead(dueAtUtc.Value, timeProvider.GetUtcNow().UtcDateTime) == false)
            .WithMessage(TodoDueDateTooFarAheadMessage);
    }

    public static IRuleBuilderOptions<T, DateTime?> NewTodoDueDate<T>(
        this IRuleBuilder<T, DateTime?> rule,
        TimeProvider timeProvider)
    {
        return rule
            .Must(dueAtUtc => dueAtUtc == null
                || IsTodoDueDateInThePast(dueAtUtc.Value, timeProvider.GetUtcNow().UtcDateTime) == false)
            .WithMessage(TodoDueDateInThePastMessage)
            .TodoDueDate(timeProvider);
    }

    public static bool IsTodoDueDateInThePast(DateTime dueAtUtc, DateTime nowUtc)
    {
        return dueAtUtc < nowUtc - TodoDueDateClockTolerance;
    }

    public static bool IsTodoDueDateTooFarAhead(DateTime dueAtUtc, DateTime nowUtc)
    {
        return dueAtUtc > nowUtc.AddYears(TodoDueDateMaximumYearsAhead);
    }

    public static IRuleBuilderOptions<T, int> EntityId<T>(this IRuleBuilder<T, int> rule)
    {
        return rule.GreaterThan(0);
    }
}
