using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApi.API.Authentication;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Infrastructure.Persistence;
using TodoApi.Infrastructure.Persistence.Auditing;

namespace todo.API.Tests;

public class AuditApiTests
{
    [Fact]
    public async Task ApiUsesTheAuthenticatedActorAndAnAdminDeletionLeavesHistoryAfterCascade()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).Options;
        using var original = new TodoApiFactory();
        using var factory = original.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TodoDbContext>();
            services.RemoveAll<DbContextOptions<TodoDbContext>>();
            services.AddSingleton(options);
            services.AddScoped<TodoDbContext>();
        }));
        int ownerId, adminId;
        string ownerToken, otherToken, adminToken;
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            TodoDbContext db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
            User owner = User.RegisterWithPassword("owner@example.com", "Owner", "secret-hash");
            User other = User.RegisterWithPassword("other@example.com", "Other", "secret-hash");
            User admin = User.RegisterWithGoogle("admin@example.com", "Admin", "secret-subject", UserRole.Admin);
            db.Users.AddRange(owner, other, admin);
            await db.SaveChangesAsync(ct);
            Assert.Null(owner.CreatedBy);
            ownerId = owner.Id;
            adminId = admin.Id;
            IAccessTokenGenerator tokens = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>();
            ownerToken = tokens.GenerateToken(owner).Value;
            otherToken = tokens.GenerateToken(other).Value;
            adminToken = tokens.GenerateToken(admin).Value;
        }

        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add(RequireClientHeaderAttribute.HeaderName, "1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        using HttpResponseMessage creation = await client.PostAsJsonAsync("/api/todo-lists", new { name = "Work" }, ct);
        Assert.Equal(HttpStatusCode.Created, creation.StatusCode);
        using var created = JsonDocument.Parse(await creation.Content.ReadAsStringAsync(ct));
        int listId = created.RootElement.GetProperty("id").GetInt32();
        Assert.False(created.RootElement.TryGetProperty("createdBy", out _));
        using HttpResponseMessage todoCreation = await client.PostAsJsonAsync($"/api/todo-lists/{listId}/todos", new { title = "Task" }, ct);
        Assert.Equal(HttpStatusCode.Created, todoCreation.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);
        using HttpResponseMessage denied = await client.PutAsJsonAsync($"/api/todo-lists/{listId}", new { name = "Stolen" }, ct);
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            TodoDbContext db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            var list = await db.TodoLists.SingleAsync(ct);
            Assert.Equal(ownerId, list.CreatedBy);
            Assert.Null(list.UpdatedBy);
            Assert.Equal("Work", list.Name);
            Assert.Equal(ownerId, (await db.TodoItems.SingleAsync(ct)).CreatedBy);
            Assert.Equal(0, await db.AuditRecords.CountAsync(ct));
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        using HttpResponseMessage rename = await client.PutAsJsonAsync($"/api/todo-lists/{listId}", new { name = "Renamed" }, ct);
        Assert.Equal(HttpStatusCode.OK, rename.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using HttpResponseMessage deletion = await client.DeleteAsync($"/api/todo-lists/{listId}", ct);
        Assert.Equal(HttpStatusCode.NoContent, deletion.StatusCode);
        await using AsyncServiceScope verificationScope = factory.Services.CreateAsyncScope();
        TodoDbContext verification = verificationScope.ServiceProvider.GetRequiredService<TodoDbContext>();
        Assert.Equal(0, await verification.TodoLists.CountAsync(ct));
        Assert.Equal(0, await verification.TodoItems.CountAsync(ct));
        AuditRecord[] history = await verification.AuditRecords.ToArrayAsync(ct);
        Assert.Equal(3, history.Length);
        Assert.Equal(ownerId, Assert.Single(history, record => record.Action == AuditAction.Updated).ActorUserId);
        Assert.All(history.Where(record => record.Action == AuditAction.Deleted), record => Assert.Equal(adminId, record.ActorUserId));
        Assert.Contains(history, record => record.Action == AuditAction.Deleted && record.EntityType == nameof(TodoList) && record.EntityId == listId);

        TodoList deletedList = await verification.TodoLists.IgnoreQueryFilters().SingleAsync(ct);
        TodoItem deletedTodo = await verification.TodoItems.IgnoreQueryFilters().SingleAsync(ct);
        Assert.True(deletedList.IsDeleted);
        Assert.True(deletedTodo.IsDeleted);
        Assert.Equal(adminId, deletedList.DeletedBy);
        Assert.Equal(adminId, deletedTodo.DeletedBy);
        Assert.NotNull(deletedList.DeletedAtUtc);
        Assert.Equal("Renamed", deletedList.Name);
        Assert.Equal("Task", deletedTodo.Title);
        using HttpResponseMessage missingList = await client.GetAsync($"/api/todo-lists/{listId}", ct);
        using HttpResponseMessage missingTask = await client.GetAsync($"/api/todo-lists/{listId}/todos/{deletedTodo.Id}", ct);
        using HttpResponseMessage repeatedDelete = await client.DeleteAsync($"/api/todo-lists/{listId}", ct);
        Assert.Equal(HttpStatusCode.NotFound, missingList.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingTask.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, repeatedDelete.StatusCode);
        Assert.Equal(3, await verification.AuditRecords.CountAsync(ct));

        var deletedOwner = await verification.Users.SingleAsync(user => user.Id == ownerId, ct);
        verification.Users.Remove(deletedOwner);
        await verification.SaveChangesAsync(ct);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        using HttpResponseMessage staleSession = await client.GetAsync("/api/todo-lists", ct);
        Assert.Equal(HttpStatusCode.Unauthorized, staleSession.StatusCode);
    }
}
