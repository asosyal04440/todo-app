using TodoApi.Application.Common.Results;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Persistence;

namespace todo.Infrastructure.Tests;

public class ReadQueryTests
{
    [Fact]
    public async Task SQLite_ReadsOnlyRequestedPagesAndKeepsOwnershipAndTrackingCorrect()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var commands = new ReadCommands();
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).AddInterceptors(commands).Options;
        await using var db = new TodoDbContext(options);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await VerifyAsync(db, commands);
    }

    [Fact]
    public async Task SQLServer_ReadsOnlyRequestedPagesAndKeepsOwnershipAndTrackingCorrect()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        var commands = new ReadCommands();
        await using TodoDbContext db = database.CreateContext(commands);
        await TodoSchemaMigrator.MigrateAsync(db, TestContext.Current.CancellationToken);
        await VerifyAsync(db, commands);
    }

    private static async Task VerifyAsync(TodoDbContext db, ReadCommands commands)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        User owner = User.RegisterWithPassword("owner@example.com", "Owner", "private-hash");
        User other = User.RegisterWithGoogle("other@example.com", "Other", "private-google-subject", UserRole.User);
        db.Users.AddRange(owner, other);
        await db.SaveChangesAsync(ct);
        var list = new TodoList("First", owner.Id);
        var empty = new TodoList("Empty", owner.Id);
        var last = new TodoList("Last", owner.Id);
        var foreign = new TodoList("Foreign", other.Id);
        TodoItem foreignTodo = foreign.AddTodo("Foreign task");
        for (int i = 1; i <= 25; i++) list.AddTodo($"Task {i}", description: $"Note {i}");
        db.TodoLists.AddRange(list, empty, last, foreign);
        await db.SaveChangesAsync(ct);
        int todoId = list.Todos.Last().Id;
        db.ChangeTracker.Clear();
        commands.Sql.Clear();

        var queries = new TodoReadQueries(db);
        var lists = await queries.GetListsAsync(owner.Id, new PageRequest(2, 2), ct);
        Assert.Equal(3, lists.TotalCount);
        Assert.Equal(2, lists.TotalPages);
        Assert.Equal(last.Id, Assert.Single(lists.Items).Id);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.DoesNotContain(commands.Sql, sql => sql.Contains("TodoItems", StringComparison.Ordinal));

        commands.Sql.Clear();
        var singleList = TestResults.Success(await queries.GetListAsync(list.Id, owner.Id, false, ct));
        Assert.Equal(list.Name, singleList!.Name);
        Assert.Single(commands.Sql);
        Assert.DoesNotContain("TodoItems", commands.Sql[0]);

        commands.Sql.Clear();
        var page = TestResults.Success(await queries.GetTodosAsync(list.Id, owner.Id, false, new PageRequest(2, 10), ct));
        Assert.NotNull(page);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(Enumerable.Range(11, 10).Select(i => $"Task {i}"), page.Items.Select(todo => todo.Title));
        Assert.Equal("Note 11", page.Items[0].Description);
        Assert.DoesNotContain(commands.Sql, sql => sql.Contains("ReminderAttemptCount", StringComparison.Ordinal)
            || sql.Contains("NormalizedTitle", StringComparison.Ordinal));
        Assert.Empty(db.ChangeTracker.Entries());

        var all = TestResults.Success(await queries.GetTodosAsync(list.Id, owner.Id, false, new PageRequest(1, 100), ct));
        Assert.Equal(25, all!.Items.Count);
        var tail = TestResults.Success(await queries.GetTodosAsync(list.Id, owner.Id, false, new PageRequest(3, 10), ct));
        Assert.Equal(5, tail!.Items.Count);
        var beyond = TestResults.Success(await queries.GetTodosAsync(list.Id, owner.Id, false, new PageRequest(99, 10), ct));
        Assert.Empty(beyond!.Items);
        Assert.Equal(25, beyond.TotalCount);
        Assert.Empty(TestResults.Success(await queries.GetTodosAsync(empty.Id, owner.Id, false, new PageRequest(), ct)).Items);
        Assert.Equal(ErrorCode.AccessDenied, (await queries.GetTodosAsync(foreign.Id, owner.Id, false, new PageRequest(), ct)).Error!.Code);
        Assert.Equal(ErrorCode.ListNotFound, (await queries.GetTodosAsync(int.MaxValue, owner.Id, false, new PageRequest(), ct)).Error!.Code);
        Assert.Equal(ErrorCode.AccessDenied, (await queries.GetListAsync(foreign.Id, owner.Id, false, ct)).Error!.Code);
        TestResults.Success(await queries.GetListAsync(foreign.Id, owner.Id, true, ct));
        TestResults.Success(await queries.GetTodosAsync(foreign.Id, owner.Id, true, new PageRequest(), ct));

        commands.Sql.Clear();
        TodoItemDto? single = TestResults.Success(await queries.GetTodoAsync(list.Id, todoId, owner.Id, false, ct));
        Assert.Equal("Task 25", single!.Title);
        Assert.Single(commands.Sql);
        Assert.Equal(ErrorCode.AccessDenied, (await queries.GetTodoAsync(foreign.Id, foreignTodo.Id, owner.Id, false, ct)).Error!.Code);
        Assert.Equal(ErrorCode.TodoNotFound, (await queries.GetTodoAsync(list.Id, foreignTodo.Id, owner.Id, true, ct)).Error!.Code);
        TestResults.Success(await queries.GetTodoAsync(foreign.Id, foreignTodo.Id, owner.Id, true, ct));
        Assert.Empty(db.ChangeTracker.Entries());

        var users = await new UserReadQueries(db).GetUsersAsync(new PageRequest(2, 1), ct);
        UserDto user = Assert.Single(users.Items);
        Assert.Equal(other.Id, user.Id);
        Assert.False(user.HasPassword);
        Assert.True(user.HasGoogleLogin);
        Assert.Equal(2, users.TotalCount);
        var firstUser = Assert.Single((await new UserReadQueries(db).GetUsersAsync(new PageRequest(1, 1), ct)).Items);
        Assert.True(firstUser.HasPassword);
        Assert.False(firstUser.HasGoogleLogin);
        Assert.Empty(db.ChangeTracker.Entries());

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queries.GetListsAsync(owner.Id, new PageRequest(), cancelled.Token));

        TodoList? editable = await new TodoListRepository(db).GetByIdWithTodosAsync(list.Id, ct);
        Assert.Equal(25, editable!.Todos.Count);
        Assert.NotEmpty(db.ChangeTracker.Entries());
        editable.Rename("Updated");
        await new EfUnitOfWork(db).SaveChangesAsync(ct);
        Assert.Equal("Updated", TestResults.Success(await queries.GetListAsync(list.Id, owner.Id, false, ct)).Name);
    }

    private sealed class ReadCommands : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
