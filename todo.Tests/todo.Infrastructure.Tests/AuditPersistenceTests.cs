using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Persistence;
using TodoApi.Infrastructure.Persistence.Auditing;

namespace todo.Infrastructure.Tests;

public class AuditPersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SQLite_CreationUpdateSystemChangesAndCascadeDeletionAreAudited(bool synchronous)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).Options;
        var actor = new Actor();
        var clock = new Clock();
        await using var db = new TodoDbContext(options, actor, clock);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await VerifyLifecycleAsync(db, actor, clock, synchronous);
    }

    [Fact]
    public async Task SQLServer_CreationUpdateSystemChangesAndCascadeDeletionAreAudited()
    {
        await using var database = await SqlServerTestDatabase.CreateAsync();
        var builder = new DbContextOptionsBuilder<TodoDbContext>();
        builder.UseTodoSqlServer(database.ConnectionString);
        var options = builder.Options;
        var actor = new Actor();
        var clock = new Clock();
        await using var db = new TodoDbContext(options, actor, clock);
        await TodoSchemaMigrator.MigrateAsync(db, TestContext.Current.CancellationToken);
        await VerifyLifecycleAsync(db, actor, clock, false);
    }

    private static async Task VerifyLifecycleAsync(TodoDbContext db, Actor actor, Clock clock, bool synchronous)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        User owner = User.RegisterWithPassword("owner@example.com", "Owner", "secret-hash");
        User admin = User.RegisterWithGoogle("admin@example.com", "Admin", "secret-subject", UserRole.Admin);
        db.Users.AddRange(owner, admin);
        await SaveAsync(db, synchronous);
        Assert.Null(owner.CreatedBy);
        Assert.Equal(clock.Now.UtcDateTime, owner.CreatedAtUtc);

        actor.Id = owner.Id;
        clock.Advance();
        DateTime createdAt = clock.Now.UtcDateTime;
        var list = new TodoList("Work", owner.Id);
        TodoItem first = list.AddTodo("First");
        TodoItem second = list.AddTodo("Second");
        db.TodoLists.Add(list);
        await SaveAsync(db, synchronous);
        foreach (var entity in new TodoApi.Domain.Common.AuditableEntity[] { list, first, second })
        {
            Assert.Equal(owner.Id, entity.CreatedBy);
            Assert.Equal(createdAt, entity.CreatedAtUtc);
            Assert.Null(entity.UpdatedAtUtc);
            Assert.Null(entity.UpdatedBy);
        }
        Assert.Empty(await db.AuditRecords.ToListAsync(ct));
        clock.Advance();
        await SaveAsync(db, synchronous);
        Assert.Null(list.UpdatedAtUtc);

        db.Entry(list).Property(value => value.CreatedBy).CurrentValue = admin.Id;
        db.Entry(list).Property(value => value.CreatedAtUtc).CurrentValue = createdAt.AddYears(-1);
        db.Entry(list).Property(value => value.UpdatedBy).CurrentValue = admin.Id;
        db.Entry(list).Property(value => value.UpdatedAtUtc).CurrentValue = createdAt.AddYears(1);
        await SaveAsync(db, synchronous);
        Assert.Equal(owner.Id, list.CreatedBy);
        Assert.Equal(createdAt, list.CreatedAtUtc);
        Assert.Null(list.UpdatedAtUtc);
        Assert.Empty(await db.AuditRecords.ToListAsync(ct));

        list.Rename("Renamed");
        await SaveAsync(db, synchronous);
        Assert.Equal(owner.Id, list.UpdatedBy);
        Assert.Equal(clock.Now.UtcDateTime, list.UpdatedAtUtc);
        AuditRecord updated = await db.AuditRecords.SingleAsync(ct);
        Assert.Equal(nameof(TodoList), updated.EntityType);
        Assert.Equal(list.Id, updated.EntityId);
        Assert.Equal(AuditAction.Updated, updated.Action);
        Assert.Equal(owner.Id, updated.ActorUserId);
        Assert.Equal(clock.Now.UtcDateTime, updated.OccurredAtUtc);

        actor.Id = null;
        clock.Advance();
        first.RecordFailedReminderAttempt(clock.Now.UtcDateTime);
        await SaveAsync(db, synchronous);
        Assert.Null(first.UpdatedBy);
        Assert.Equal(clock.Now.UtcDateTime, first.UpdatedAtUtc);
        Assert.Null((await db.AuditRecords.SingleAsync(record => record.EntityType == nameof(TodoItem), ct)).ActorUserId);

        actor.Id = admin.Id;
        clock.Advance();
        db.TodoLists.Remove(list);
        await SaveAsync(db, synchronous);
        Assert.Empty(await db.TodoLists.ToListAsync(ct));
        Assert.Empty(await db.TodoItems.ToListAsync(ct));
        AuditRecord[] deleted = await db.AuditRecords.Where(record => record.Action == AuditAction.Deleted).ToArrayAsync(ct);
        Assert.Equal(3, deleted.Length);
        Assert.All(deleted, record =>
        {
            Assert.Equal(admin.Id, record.ActorUserId);
            Assert.Equal(clock.Now.UtcDateTime, record.OccurredAtUtc);
            Assert.Equal(DateTimeKind.Utc, record.OccurredAtUtc.Kind);
        });
        Assert.Contains(deleted, record => record.EntityType == nameof(TodoItem) && record.EntityId == second.Id);
        db.TodoLists.Add(new TodoList("Renamed", owner.Id));
        await SaveAsync(db, synchronous);
        Assert.Equal(5, await db.AuditRecords.CountAsync(ct));

        db.AuditRecords.Remove(updated);
        if (synchronous) Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        else await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync(ct));
        Assert.Equal(5, await db.AuditRecords.AsNoTracking().CountAsync(ct));
    }

    [Theory]
    [InlineData("UPDATE \"TodoLists\"")]
    [InlineData("INSERT INTO \"AuditRecords\"")]
    public async Task FailedBusinessOrAuditWriteRollsBackBothAndRetryDoesNotDuplicateHistory(string failingCommand)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var failure = new FailOnce(failingCommand);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).AddInterceptors(failure).Options;
        var actor = new Actor();
        var clock = new Clock();
        await using var db = new TodoDbContext(options, actor, clock);
        await db.Database.EnsureCreatedAsync(ct);
        var owner = User.RegisterWithPassword("retry@example.com", "Owner", "hash");
        db.Users.Add(owner);
        await db.SaveChangesAsync(ct);
        var list = new TodoList("Original", owner.Id);
        db.TodoLists.Add(list);
        await db.SaveChangesAsync(ct);

        actor.Id = owner.Id;
        list.Rename("Changed");
        failure.Armed = true;
        await Assert.ThrowsAsync<DataAccessException>(() => db.SaveChangesAsync(ct));
        Assert.Equal("Original", (await db.TodoLists.AsNoTracking().SingleAsync(ct)).Name);
        Assert.Equal(0, await db.AuditRecords.AsNoTracking().CountAsync(ct));
        Assert.DoesNotContain(db.ChangeTracker.Entries<AuditRecord>(), entry => entry.State == EntityState.Added);

        clock.Advance();
        await db.SaveChangesAsync(ct);
        Assert.Equal("Changed", (await db.TodoLists.AsNoTracking().SingleAsync(ct)).Name);
        AuditRecord record = await db.AuditRecords.AsNoTracking().SingleAsync(ct);
        Assert.Equal(owner.Id, record.ActorUserId);
        Assert.Equal(clock.Now.UtcDateTime, record.OccurredAtUtc);
    }

    [Fact]
    public async Task SQLServer_MigrationPreservesLegacyCreationDatesWithoutInventingActorsOrHistory()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await using var database = await SqlServerTestDatabase.CreateAsync();
        await using var db = database.CreateContext();
        await db.GetService<IMigrator>().MigrateAsync(TodoSchemaMigrator.EnforcementMigration, ct);
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO Users (Email,DisplayName,PasswordHash,Role,CreatedAtUtc)
            VALUES ('legacy@example.com','Legacy','legacy-hash',0,'2020-01-02T03:04:05');
            INSERT INTO TodoLists (Name,NormalizedName,OwnerUserId,CreatedAtUtc)
            VALUES ('Legacy','LEGACY',1,'2020-01-02T03:04:05');
            """, ct);
        await TodoSchemaMigrator.MigrateAsync(db, ct);
        TodoList list = await db.TodoLists.SingleAsync(ct);
        Assert.Equal(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), list.CreatedAtUtc);
        Assert.Null(list.CreatedBy);
        Assert.Null(list.UpdatedBy);
        Assert.Null(list.UpdatedAtUtc);
        Assert.Empty(await db.AuditRecords.ToListAsync(ct));
    }

    private static async Task SaveAsync(TodoDbContext db, bool synchronous)
    {
        if (synchronous) db.SaveChanges();
        else await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private sealed class Actor : ICurrentUser
    {
        public int? Id { get; set; }
        public int UserId => Id ?? throw new InvalidOperationException("No signed-in user");
        public int? UserIdOrNull => Id;
        public bool IsAdmin => false;
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; private set; } = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
        public void Advance() => Now = Now.AddMinutes(1);
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
