using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Contracts;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Specifications;
using TodoApi.Infrastructure.Persistence;
using TodoApi.Infrastructure.Persistence.Auditing;

namespace todo.Infrastructure.Tests;

public class SoftDeletionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task DeletingTaskAndUnloadedListPreservesRowsFiltersPagesAndRemindersAndReleasesNames(
        bool sqlServer, bool synchronous)
    {
        await using var store = await Store.CreateAsync(sqlServer);
        await using var db = store.CreateContext();
        CancellationToken ct = TestContext.Current.CancellationToken;
        var owner = User.RegisterWithPassword("owner@example.com", "Owner", "hash");
        db.Users.Add(owner);
        await db.SaveChangesAsync(ct);
        store.Actor.Id = owner.Id;
        var list = new TodoList("Work", owner.Id);
        var first = list.AddTodo("First", "Preserve this text", store.Clock.Now.UtcDateTime.AddMinutes(30));
        var second = list.AddTodo("Second", dueAtUtc: store.Clock.Now.UtcDateTime.AddMinutes(40));
        db.TodoLists.Add(list);
        await db.SaveChangesAsync(ct);
        DateTime createdAt = first.CreatedAtUtc;
        store.Clock.Advance();
        Assert.True(list.RemoveTodo(first.Id));
        await SaveAsync(db, synchronous);
        DateTime firstDeletedAt = store.Clock.Now.UtcDateTime;
        db.ChangeTracker.Clear();
        var tombstone = await db.TodoItems.IgnoreQueryFilters().AsNoTracking().SingleAsync(todo => todo.Id == first.Id, ct);
        AssertDeleted(tombstone, owner.Id, firstDeletedAt);
        Assert.Equal("Preserve this text", tombstone.Description);
        Assert.Equal(list.Id, tombstone.TodoListId);
        Assert.Equal(createdAt, tombstone.CreatedAtUtc);
        var page = await new TodoReadQueries(db).GetTodosAsync(list.Id, owner.Id, false, new PageRequest(1, 20), ct);
        Assert.True(page.IsSuccess);
        Assert.Equal(1, page.Value.TotalCount);
        Assert.Equal(second.Id, Assert.Single(page.Value.Items).Id);
        var specification = new TodoReminderSpecification(store.Clock.Now.UtcDateTime, TimeSpan.FromHours(1), 3, TimeSpan.FromMinutes(15));
        Assert.False(specification.IsSatisfiedBy(tombstone));
        var reminders = await new TodoReminderRepository(db).GetTodosDueForReminderAsync(specification, 20, ct);
        Assert.Equal(second.Id, Assert.Single(reminders).Todo.Id);

        var loaded = await db.TodoLists.Include(value => value.Todos).SingleAsync(ct);
        loaded.AddTodo("First");
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        loaded = await db.TodoLists.SingleAsync(ct);
        Assert.Empty(loaded.Todos);
        store.Clock.Advance();
        db.TodoLists.Remove(loaded);
        await SaveAsync(db, synchronous);
        db.ChangeTracker.Clear();
        Assert.Empty(await db.TodoLists.ToListAsync(ct));
        Assert.Empty(await db.TodoItems.ToListAsync(ct));
        var all = await db.TodoItems.IgnoreQueryFilters().AsNoTracking().ToListAsync(ct);
        Assert.Equal(3, all.Count);
        Assert.All(all, todo => Assert.True(todo.IsDeleted));
        Assert.Equal(firstDeletedAt, all.Single(todo => todo.Id == first.Id).DeletedAtUtc);
        AssertDeleted(await db.TodoLists.IgnoreQueryFilters().SingleAsync(ct), owner.Id, store.Clock.Now.UtcDateTime);
        Assert.Empty(await new TodoReminderRepository(db).GetTodosDueForReminderAsync(specification, 20, ct));
        db.TodoLists.Add(new TodoList("Work", owner.Id));
        await db.SaveChangesAsync(ct);
        Assert.Equal(1, await db.TodoLists.CountAsync(ct));
        Assert.Equal(2, await db.TodoLists.IgnoreQueryFilters().CountAsync(ct));
        Assert.Equal(4, await db.AuditRecords.CountAsync(record => record.Action == AuditAction.Deleted, ct));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UserDeletionPreservesItsUnloadedGraphAndRevokesTokens(bool sqlServer)
    {
        await using var store = await Store.CreateAsync(sqlServer);
        await using var db = store.CreateContext();
        CancellationToken ct = TestContext.Current.CancellationToken;
        var owner = User.RegisterWithGoogle("owner@example.com", "Owner", "google-subject", UserRole.User);
        db.Users.Add(owner);
        await db.SaveChangesAsync(ct);
        var list = new TodoList("Work", owner.Id);
        list.AddTodo("Task");
        db.TodoLists.Add(list);
        var token = new RefreshToken(owner, new string('a', 64), store.Clock.Now.UtcDateTime, store.Clock.Now.UtcDateTime.AddDays(1));
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        owner = await db.Users.SingleAsync(ct);
        store.Actor.Id = owner.Id;
        db.Users.Remove(owner);
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        Assert.Null(await new UserRepository(db).GetByIdAsync(owner.Id, ct));
        Assert.Empty(await db.TodoLists.ToListAsync(ct));
        Assert.Empty(await db.TodoItems.ToListAsync(ct));
        Assert.Empty(await db.RefreshTokens.ToListAsync(ct));
        AssertDeleted(await db.Users.IgnoreQueryFilters().SingleAsync(ct), owner.Id, store.Clock.Now.UtcDateTime);
        AssertDeleted(await db.TodoLists.IgnoreQueryFilters().SingleAsync(ct), owner.Id, store.Clock.Now.UtcDateTime);
        AssertDeleted(await db.TodoItems.IgnoreQueryFilters().SingleAsync(ct), owner.Id, store.Clock.Now.UtcDateTime);
        Assert.True((await db.RefreshTokens.IgnoreQueryFilters().SingleAsync(ct)).IsRevoked);
        db.Users.Add(User.RegisterWithGoogle("owner@example.com", "New account", "google-subject", UserRole.User));
        await db.SaveChangesAsync(ct);
        Assert.NotEqual(owner.Id, (await db.Users.SingleAsync(ct)).Id);
        Assert.Empty(await db.RefreshTokens.ToListAsync(ct));
    }

    [Theory]
    [InlineData("UPDATE \"TodoLists\"")]
    [InlineData("INSERT INTO \"AuditRecords\"")]
    public async Task FailedDeletionRollsBackAndRetryKeepsOneDeletionHistory(string commandPrefix)
    {
        var failure = new FailOnce(commandPrefix);
        await using var store = await Store.CreateAsync(false);
        await using var db = store.CreateContext(failure);
        CancellationToken ct = TestContext.Current.CancellationToken;
        var owner = User.RegisterWithPassword("owner@example.com", "Owner", "hash");
        db.Users.Add(owner);
        await db.SaveChangesAsync(ct);
        var list = new TodoList("Work", owner.Id);
        list.AddTodo("Task");
        db.TodoLists.Add(list);
        await db.SaveChangesAsync(ct);
        store.Actor.Id = owner.Id;
        db.TodoLists.Remove(list);
        failure.Armed = true;
        await Assert.ThrowsAsync<DataAccessException>(() => db.SaveChangesAsync(ct));
        Assert.False((await db.TodoLists.IgnoreQueryFilters().AsNoTracking().SingleAsync(ct)).IsDeleted);
        Assert.False((await db.TodoItems.IgnoreQueryFilters().AsNoTracking().SingleAsync(ct)).IsDeleted);
        Assert.Empty(await db.AuditRecords.AsNoTracking().ToListAsync(ct));
        store.Clock.Advance();
        await db.SaveChangesAsync(ct);
        db.ChangeTracker.Clear();
        AssertDeleted(await db.TodoLists.IgnoreQueryFilters().SingleAsync(ct), owner.Id, store.Clock.Now.UtcDateTime);
        Assert.Equal(2, await db.AuditRecords.CountAsync(record => record.Action == AuditAction.Deleted, ct));
        Assert.Equal(0, await db.AuditRecords.CountAsync(record => record.Action == AuditAction.Updated, ct));
    }

    [Fact]
    public async Task AStaleUpdateCannotOverwriteADeletion()
    {
        await using var store = await Store.CreateAsync(false);
        await using var db = store.CreateContext();
        CancellationToken ct = TestContext.Current.CancellationToken;
        var owner = User.RegisterWithPassword("owner@example.com", "Owner", "hash");
        db.Users.Add(owner);
        await db.SaveChangesAsync(ct);
        var list = new TodoList("Work", owner.Id);
        db.TodoLists.Add(list);
        await db.SaveChangesAsync(ct);
        await using var stale = store.CreateContext();
        var copy = await stale.TodoLists.SingleAsync(ct);
        db.TodoLists.Remove(list);
        await db.SaveChangesAsync(ct);
        copy.Rename("Stale rename");
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => stale.SaveChangesAsync(ct));
        var saved = await db.TodoLists.IgnoreQueryFilters().AsNoTracking().SingleAsync(ct);
        Assert.True(saved.IsDeleted);
        Assert.Equal("Work", saved.Name);
    }

    private static async Task SaveAsync(TodoDbContext db, bool synchronous)
    {
        if (synchronous) db.SaveChanges();
        else await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static void AssertDeleted(AuditableEntity entity, int actorId, DateTime at)
    {
        Assert.True(entity.IsDeleted);
        Assert.Equal(actorId, entity.DeletedBy);
        Assert.Equal(at, entity.DeletedAtUtc);
        Assert.Equal(DateTimeKind.Utc, entity.DeletedAtUtc!.Value.Kind);
    }

    private sealed class Actor : ICurrentUser
    {
        public int? Id { get; set; }
        public int UserId => Id ?? throw new InvalidOperationException();
        public int? UserIdOrNull => Id;
        public bool IsAdmin => false;
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; private set; } = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance() => Now = Now.AddMinutes(1);
    }

    private sealed class Store(SqliteConnection? sqlite, SqlServerTestDatabase? sql) : IAsyncDisposable
    {
        public Actor Actor { get; } = new();
        public Clock Clock { get; } = new();
        public TodoDbContext CreateContext(params IInterceptor[] interceptors)
        {
            var options = new DbContextOptionsBuilder<TodoDbContext>();
            if (sqlite != null) options.UseSqlite(sqlite);
            else options.UseTodoSqlServer(sql!.ConnectionString);
            options.AddInterceptors(interceptors);
            return new TodoDbContext(options.Options, Actor, Clock);
        }
        public static async Task<Store> CreateAsync(bool sqlServer)
        {
            if (sqlServer)
            {
                var database = await SqlServerTestDatabase.CreateAsync();
                var store = new Store(null, database);
                try
                {
                    await using var db = store.CreateContext();
                    await TodoSchemaMigrator.MigrateAsync(db, TestContext.Current.CancellationToken);
                    return store;
                }
                catch { await store.DisposeAsync(); throw; }
            }
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var sqliteStore = new Store(connection, null);
            await using var context = sqliteStore.CreateContext();
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return sqliteStore;
        }
        public async ValueTask DisposeAsync()
        {
            if (sqlite != null) await sqlite.DisposeAsync();
            if (sql != null) await sql.DisposeAsync();
        }
    }

    private sealed class FailOnce(string commandPrefix) : DbCommandInterceptor
    {
        public bool Armed { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Armed && command.CommandText.StartsWith(commandPrefix, StringComparison.Ordinal))
            {
                Armed = false;
                throw new DbUpdateException("Injected write failure", new InvalidOperationException());
            }
            return ValueTask.FromResult(result);
        }
    }
}
