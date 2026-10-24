using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Specifications;
using TodoApi.Infrastructure.Persistence;

namespace todo.Infrastructure.Tests;

public class TodoReminderSpecificationTests
{
    private static readonly DateTime NowUtc = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan LeadTime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task SQLite_QueryAndDomainAgreeAtBoundariesAndFilterBeforeTakingTheBatch()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).Options;
        await using var context = new TodoDbContext(options);
        await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

        await VerifySelectionAsync(context);
    }

    [Fact]
    public async Task SQLServer_QueryAndDomainAgreeAtBoundariesAndFilterBeforeTakingTheBatch()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        await using TodoDbContext context = database.CreateContext();
        await TodoSchemaMigrator.MigrateAsync(context, TestContext.Current.CancellationToken);

        await VerifySelectionAsync(context);
    }

    private static async Task VerifySelectionAsync(TodoDbContext context)
    {
        User owner = User.RegisterWithPassword("reminders@example.com", "Owner", "hash");
        context.Users.Add(owner);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var list = new TodoList("Reminders", owner.Id);

        TodoItem Add(string title, DateTime? dueAtUtc) => list.AddTodo(title, dueAtUtc: dueAtUtc);
        Add("No due date", null);
        Add("Expired", NowUtc.AddTicks(-1));
        Add("Due now", NowUtc);
        Add("Just after now", NowUtc.AddTicks(1));
        Add("At lead time", NowUtc.Add(LeadTime));
        Add("After lead time", NowUtc.Add(LeadTime).AddTicks(1));
        Add("Completed", NowUtc.AddMinutes(1)).MarkCompleted();
        Add("Already sent", NowUtc.AddMinutes(1)).MarkReminderSent(NowUtc.AddMinutes(-30));
        Add("Retry too soon", NowUtc.AddMinutes(1)).RecordFailedReminderAttempt(NowUtc.AddMinutes(-14));
        Add("Before retry boundary", NowUtc.AddMinutes(2)).RecordFailedReminderAttempt(NowUtc.Subtract(RetryDelay).AddTicks(1));
        Add("At retry boundary", NowUtc.AddMinutes(3)).RecordFailedReminderAttempt(NowUtc.Subtract(RetryDelay));
        Add("After retry boundary", NowUtc.AddMinutes(4)).RecordFailedReminderAttempt(NowUtc.Subtract(RetryDelay).AddTicks(-1));
        TodoItem thirdAttempt = Add("Third attempt allowed", NowUtc.AddMinutes(5));
        thirdAttempt.RecordFailedReminderAttempt(NowUtc.AddMinutes(-40));
        thirdAttempt.RecordFailedReminderAttempt(NowUtc.AddMinutes(-20));
        TodoItem exhausted = Add("Attempts exhausted", NowUtc.AddMinutes(1));
        for (int i = 0; i < 3; i++) exhausted.RecordFailedReminderAttempt(NowUtc.AddMinutes(-20));
        Add("No attempts yet", NowUtc.AddMinutes(6));
        Add("Due in 45 minutes", NowUtc.AddMinutes(45));

        context.TodoLists.Add(list);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        context.ChangeTracker.Clear();

        TodoItem[] todos = await context.TodoItems.OrderBy(todo => todo.DueAtUtc)
            .ThenBy(todo => todo.Id).ToArrayAsync(TestContext.Current.CancellationToken);
        var specification = new TodoReminderSpecification(NowUtc, LeadTime, 3, RetryDelay);
        string[] expected = ["Just after now", "At retry boundary", "After retry boundary",
            "Third attempt allowed", "No attempts yet", "Due in 45 minutes", "At lead time"];

        Assert.Equal(expected, todos.Where(specification.IsSatisfiedBy).Select(todo => todo.Title));
        foreach (TodoItem todo in todos)
        {
            bool eligible = expected.Contains(todo.Title);
            Assert.Equal(eligible, specification.IsSatisfiedBy(todo));
            Assert.Equal(eligible, todo.NeedsReminder(NowUtc, LeadTime)
                && todo.CanRetryReminder(NowUtc, 3, RetryDelay));
        }

        var repository = new TodoReminderRepository(context);
        var candidates = await repository.GetTodosDueForReminderAsync(
            specification, 100, TestContext.Current.CancellationToken);
        Assert.Equal(expected, candidates.Select(candidate => candidate.Todo.Title));
        Assert.All(candidates, candidate =>
        {
            Assert.Equal(owner.Id, candidate.OwnerUserId);
            Assert.Equal(owner.Email, candidate.OwnerEmail);
            Assert.Equal(list.Name, candidate.ListName);
        });

        var batch = await repository.GetTodosDueForReminderAsync(specification, 2, TestContext.Current.CancellationToken);
        Assert.Equal(expected.Take(2), batch.Select(candidate => candidate.Todo.Title));

        var changedSettings = new TodoReminderSpecification(NowUtc, TimeSpan.FromMinutes(30), 2, RetryDelay);
        string[] changedExpected = ["Just after now", "At retry boundary", "After retry boundary", "No attempts yet"];
        Assert.Equal(changedExpected, todos.Where(changedSettings.IsSatisfiedBy).Select(todo => todo.Title));
        var changedCandidates = await repository.GetTodosDueForReminderAsync(changedSettings, 100, TestContext.Current.CancellationToken);
        Assert.Equal(changedExpected, changedCandidates.Select(candidate => candidate.Todo.Title));

        candidates[0].Todo.MarkReminderSent(NowUtc);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var nextBatch = await repository.GetTodosDueForReminderAsync(specification, 2, TestContext.Current.CancellationToken);
        Assert.Equal(expected.Skip(1).Take(2), nextBatch.Select(candidate => candidate.Todo.Title));
    }
}
