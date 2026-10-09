using TodoApi.Domain.Entities;

namespace todo.Domain.Tests;

public class TodoReminderRuleTests
{
    private static readonly DateTime NowUtc = new DateTime(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan LeadTime = TimeSpan.FromHours(24);

    [Fact]
    public void NeedsReminder_IsTrueWhenTheDueDateIsWithinTheLeadTime()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));

        Assert.True(todo.NeedsReminder(NowUtc, LeadTime));
    }

    [Fact]
    public void NeedsReminder_IsFalseWhenTheDueDateIsStillFarAway()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(30));

        Assert.False(todo.NeedsReminder(NowUtc, LeadTime));
    }

    [Fact]
    public void NeedsReminder_IsFalseWhenTheDueDateHasAlreadyPassed()
    {
        TodoItem todo = CreateTodo(NowUtc.AddMinutes(-5));

        Assert.False(todo.NeedsReminder(NowUtc, LeadTime));
    }

    [Fact]
    public void NeedsReminder_IsFalseWithoutADueDate()
    {
        TodoItem todo = CreateTodo(null);

        Assert.False(todo.NeedsReminder(NowUtc, LeadTime));
    }

    [Fact]
    public void NeedsReminder_IsFalseForACompletedTodo()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));

        todo.MarkCompleted();

        Assert.False(todo.NeedsReminder(NowUtc, LeadTime));
    }

    [Fact]
    public void NeedsReminder_IsFalseAfterTheReminderWasSent()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));

        todo.MarkReminderSent(NowUtc);

        Assert.False(todo.NeedsReminder(NowUtc, LeadTime));
        Assert.Equal(NowUtc, todo.ReminderSentAtUtc);
    }

    [Fact]
    public void SetDueAtUtc_AllowsANewReminderWhenTheDueDateChanges()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));
        todo.MarkReminderSent(NowUtc);

        todo.SetDueAtUtc(NowUtc.AddHours(10));

        Assert.Null(todo.ReminderSentAtUtc);
        Assert.True(todo.NeedsReminder(NowUtc, LeadTime));
    }

    [Fact]
    public void SetDueAtUtc_KeepsTheReminderWhenTheSameDueDateIsSavedAgain()
    {
        DateTime dueAtUtc = NowUtc.AddHours(3);
        TodoItem todo = CreateTodo(dueAtUtc);
        todo.MarkReminderSent(NowUtc);

        todo.SetDueAtUtc(dueAtUtc);

        Assert.Equal(NowUtc, todo.ReminderSentAtUtc);
    }

    [Fact]
    public void CanRetryReminder_WaitsForTheRetryDelayAfterAFailedAttempt()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));

        todo.RecordFailedReminderAttempt(NowUtc);

        Assert.Equal(1, todo.ReminderAttemptCount);
        Assert.False(todo.CanRetryReminder(NowUtc.AddMinutes(14), maximumAttempts: 3, TimeSpan.FromMinutes(15)));
        Assert.True(todo.CanRetryReminder(NowUtc.AddMinutes(15), maximumAttempts: 3, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void CanRetryReminder_GivesUpAfterTheMaximumNumberOfAttempts()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));

        todo.RecordFailedReminderAttempt(NowUtc);
        todo.RecordFailedReminderAttempt(NowUtc.AddMinutes(15));
        todo.RecordFailedReminderAttempt(NowUtc.AddMinutes(30));

        Assert.False(todo.CanRetryReminder(NowUtc.AddHours(2), maximumAttempts: 3, TimeSpan.FromMinutes(15)));
    }

    [Fact]
    public void SetDueAtUtc_GivesAFreshSetOfAttemptsWhenTheDueDateChanges()
    {
        TodoItem todo = CreateTodo(NowUtc.AddHours(3));
        todo.RecordFailedReminderAttempt(NowUtc);
        todo.RecordFailedReminderAttempt(NowUtc);
        todo.RecordFailedReminderAttempt(NowUtc);

        todo.SetDueAtUtc(NowUtc.AddHours(10));

        Assert.Equal(0, todo.ReminderAttemptCount);
        Assert.Null(todo.ReminderLastAttemptAtUtc);
        Assert.True(todo.CanRetryReminder(NowUtc, maximumAttempts: 3, TimeSpan.FromMinutes(15)));
    }

    private static TodoItem CreateTodo(DateTime? dueAtUtc)
    {
        TodoList todoList = new TodoList("İş", 1);

        return todoList.AddTodo("Raporu teslim et", dueAtUtc: dueAtUtc);
    }
}
