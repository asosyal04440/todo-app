using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;
using TodoApi.Infrastructure.Persistence;

namespace todo.Infrastructure.Tests;

public class SqlServerNameRulesTests
{
    [Fact]
    public async Task NormalizedKeys_AlignDomainLookupAndBothUniqueIndexes()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        await using TodoDbContext setup = database.CreateContext();
        await TodoSchemaMigrator.MigrateAsync(setup, TestContext.Current.CancellationToken);

        (string First, string Second, bool Same)[] cases =
        [
            ("Plan", " plan ", true), ("I", "ı", true), ("İ", "i", true), ("I", "i", false),
            ("ISIK", "ışık", false), ("IŞIK", "ışık", true), ("İzmir", "izmir", true), ("Café", "Cafe\u0301", true),
            ("ş", "s", false), ("ß", "ss", false), ("œ", "oe", false),
            ("Plan", "Pl\u200dan", false), ("Plan", "Pl\0an", false),
            ("  Ev\u00a0", "ev", true)
        ];

        for (int i = 0; i < cases.Length; i++)
        {
            var pair = cases[i];
            Assert.Equal(pair.Same, NameComparison.AreSame(pair.First, pair.Second));
            User owner = User.RegisterWithPassword($"case{i}@example.com", "Owner", "hash");
            setup.Users.Add(owner);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);

            TodoList list = new TodoList(pair.First, owner.Id);
            TodoItem firstTodo = list.AddTodo(pair.First);
            setup.TodoLists.Add(list);
            await setup.SaveChangesAsync(TestContext.Current.CancellationToken);

            await using TodoDbContext lookup = database.CreateContext();
            var repository = new TodoListRepository(lookup);
            Assert.Equal(pair.Same, await repository.ExistsWithNameForOwnerAsync(
                owner.Id, pair.Second, null, TestContext.Current.CancellationToken));

            await using TodoDbContext listWriter = database.CreateContext();
            listWriter.TodoLists.Add(new TodoList(pair.Second, owner.Id));
            if (pair.Same)
            {
                await Assert.ThrowsAsync<ConflictException>(() => listWriter.SaveChangesAsync(TestContext.Current.CancellationToken));
            }
            else
            {
                await listWriter.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            await using TodoDbContext todoWriter = database.CreateContext();
            TodoList independentlyLoadedList = await todoWriter.TodoLists.SingleAsync(x => x.Id == list.Id, TestContext.Current.CancellationToken);
            Assert.Empty(independentlyLoadedList.Todos);
            independentlyLoadedList.AddTodo(pair.Second);
            if (pair.Same)
            {
                await Assert.ThrowsAsync<ConflictException>(() => todoWriter.SaveChangesAsync(TestContext.Current.CancellationToken));
            }
            else
            {
                await todoWriter.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            Assert.Equal(pair.First.Trim(), firstTodo.Title);
        }
    }

    [Fact]
    public async Task TwoRequestsPassingThePrecheck_StillSaveOnlyOneList()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        await using TodoDbContext setup = database.CreateContext();
        await TodoSchemaMigrator.MigrateAsync(setup, TestContext.Current.CancellationToken);
        User owner = User.RegisterWithPassword("race@example.com", "Owner", "hash");
        setup.Users.Add(owner);
        await setup.SaveChangesAsync(TestContext.Current.CancellationToken);

        await using TodoDbContext first = database.CreateContext();
        await using TodoDbContext second = database.CreateContext();
        Assert.False(await new TodoListRepository(first).ExistsWithNameForOwnerAsync(
            owner.Id, "Plan", null, TestContext.Current.CancellationToken));
        Assert.False(await new TodoListRepository(second).ExistsWithNameForOwnerAsync(
            owner.Id, "plan", null, TestContext.Current.CancellationToken));
        first.TodoLists.Add(new TodoList("Plan", owner.Id));
        second.TodoLists.Add(new TodoList("plan", owner.Id));

        bool[] results = await Task.WhenAll(TrySaveAsync(first), TrySaveAsync(second));

        Assert.Single(results, saved => saved);
        Assert.Equal(1, await setup.TodoLists.CountAsync(x => x.OwnerUserId == owner.Id, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Backfill_PreservesExistingNamesIdsAndRelationsAndCanRunTwice()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        await using TodoDbContext context = database.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync("AddReminderRetryTracking", TestContext.Current.CancellationToken);
        int ownerId = await AddOwnerAsync(context);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [TodoLists] ([Name], [OwnerUserId], [CreatedAtUtc]) VALUES ({"İş"}, {ownerId}, SYSUTCDATETIME());
            INSERT INTO [TodoItems] ([TodoListId], [Title], [IsCompleted], [CreatedAtUtc], [Priority])
                SELECT [Id], {"Cafe\u0301"}, 0, SYSUTCDATETIME(), 1 FROM [TodoLists];
            """, TestContext.Current.CancellationToken);

        int originalListId = await context.Database.SqlQuery<int>($"SELECT [Id] AS [Value] FROM [TodoLists]")
            .SingleAsync(TestContext.Current.CancellationToken);
        int originalTodoId = await context.Database.SqlQuery<int>($"SELECT [Id] AS [Value] FROM [TodoItems]")
            .SingleAsync(TestContext.Current.CancellationToken);

        await TodoSchemaMigrator.MigrateAsync(context, TestContext.Current.CancellationToken);
        await TodoSchemaMigrator.MigrateAsync(context, TestContext.Current.CancellationToken);

        TodoList list = await context.TodoLists.Include(x => x.Todos).SingleAsync(TestContext.Current.CancellationToken);
        TodoItem todo = Assert.Single(list.Todos);
        Assert.Equal(originalListId, list.Id);
        Assert.Equal(originalTodoId, todo.Id);
        Assert.Equal(ownerId, list.OwnerUserId);
        Assert.Equal(list.Id, todo.TodoListId);
        Assert.Equal("İş", list.Name);
        Assert.Equal("İŞ", list.NormalizedName);
        Assert.Equal("Cafe\u0301", todo.Title);
        Assert.Equal("CAFÉ", todo.NormalizedTitle);
        Assert.False(context.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task BackfillConflict_PreservesRowsAndOldIndexesThenCanBeRetried()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        await using TodoDbContext context = database.CreateContext();
        await context.GetService<IMigrator>().MigrateAsync("AddReminderRetryTracking", TestContext.Current.CancellationToken);
        int ownerId = await AddOwnerAsync(context);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [TodoLists] ([Name], [OwnerUserId], [CreatedAtUtc])
                VALUES ({"Plan"}, {ownerId}, SYSUTCDATETIME()), ({"plan"}, {ownerId}, SYSUTCDATETIME());
            """, TestContext.Current.CancellationToken);

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => TodoSchemaMigrator.MigrateAsync(context, TestContext.Current.CancellationToken));
        Assert.Contains("Name normalization conflict", error.Message);
        Assert.Equal(2, await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [TodoLists]")
            .SingleAsync(TestContext.Current.CancellationToken));
        IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        Assert.DoesNotContain(applied, id => id.EndsWith("_" + TodoSchemaMigrator.EnforcementMigration));
        await using SqlConnection sql = new SqlConnection(database.ConnectionString);
        await sql.OpenAsync(TestContext.Current.CancellationToken);
        await using SqlCommand indexCheck = new SqlCommand(
            "SELECT COUNT(*) FROM sys.indexes WHERE name = 'IX_TodoLists_OwnerUserId_Name'", sql);
        Assert.Equal(1, (int)(await indexCheck.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);
        await using SqlCommand nullCheck = new SqlCommand(
            "SELECT COUNT(*) FROM [TodoLists] WHERE [NormalizedName] IS NULL", sql);
        Assert.Equal(2, (int)(await nullCheck.ExecuteScalarAsync(TestContext.Current.CancellationToken))!);

        await context.Database.ExecuteSqlInterpolatedAsync($"UPDATE [TodoLists] SET [Name] = {"Other"} WHERE [Name] = {"plan"}", TestContext.Current.CancellationToken);
        await TodoSchemaMigrator.MigrateAsync(context, TestContext.Current.CancellationToken);
        Assert.Equal(2, await context.TodoLists.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DirectMigrationWithoutBackfill_FailsBeforeDroppingTheOldIndex()
    {
        await using SqlServerTestDatabase database = await SqlServerTestDatabase.CreateAsync();
        await using TodoDbContext context = database.CreateContext();
        IMigrator migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("AddReminderRetryTracking", TestContext.Current.CancellationToken);
        int ownerId = await AddOwnerAsync(context);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [TodoLists] ([Name], [OwnerUserId], [CreatedAtUtc]) VALUES ({"Plan"}, {ownerId}, SYSUTCDATETIME())
            """, TestContext.Current.CancellationToken);

        SqlException error = await Assert.ThrowsAsync<SqlException>(() => migrator.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(51000, error.Number);
        Assert.Equal(1, await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM [TodoLists]")
            .SingleAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken),
            id => id.EndsWith("_" + TodoSchemaMigrator.EnforcementMigration));
    }

    private static async Task<int> AddOwnerAsync(TodoDbContext context)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [Users] ([Email], [DisplayName], [PasswordHash], [Role], [CreatedAtUtc])
                VALUES ({"old@example.com"}, {"Owner"}, {"hash"}, {(int)TodoApi.Domain.Enums.UserRole.User}, SYSUTCDATETIME());
            """, TestContext.Current.CancellationToken);
        return await context.Database.SqlQuery<int>($"SELECT [Id] AS [Value] FROM [Users] WHERE [Email] = {"old@example.com"}")
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<bool> TrySaveAsync(TodoDbContext context)
    {
        try { await context.SaveChangesAsync(TestContext.Current.CancellationToken); return true; }
        catch (ConflictException) { return false; }
    }

}
