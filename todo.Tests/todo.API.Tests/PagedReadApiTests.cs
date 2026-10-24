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

namespace todo.API.Tests;

public class PagedReadApiTests
{
    [Fact]
    public async Task ReadEndpointsPageResultsRejectInvalidBoundsAndKeepAuthorization()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(ct);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).Options;
        using var originalFactory = new TodoApiFactory();
        using WebApplicationFactory<Program> factory = originalFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TodoDbContext>();
                services.RemoveAll<DbContextOptions<TodoDbContext>>();
                services.AddSingleton(options);
                services.AddScoped<TodoDbContext>();
            }));

        int listId, foreignListId, foreignTodoId;
        string ownerToken, adminToken;
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            TodoDbContext db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            await db.Database.EnsureCreatedAsync(ct);
            User owner = User.RegisterWithPassword("reader@example.com", "Reader", "private-hash");
            User admin = User.RegisterWithGoogle("admin@example.com", "Admin", "private-subject", UserRole.Admin);
            db.Users.AddRange(owner, admin);
            await db.SaveChangesAsync(ct);
            var list = new TodoList("Own list", owner.Id);
            for (int i = 1; i <= 21; i++) list.AddTodo($"Task {i}");
            var foreign = new TodoList("Foreign list", admin.Id);
            var foreignTodo = foreign.AddTodo("Foreign task");
            db.TodoLists.AddRange(list, foreign);
            await db.SaveChangesAsync(ct);
            listId = list.Id;
            foreignListId = foreign.Id;
            foreignTodoId = foreignTodo.Id;
            IAccessTokenGenerator tokens = scope.ServiceProvider.GetRequiredService<IAccessTokenGenerator>();
            ownerToken = tokens.GenerateToken(owner).Value;
            adminToken = tokens.GenerateToken(admin).Value;
        }

        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Add(RequireClientHeaderAttribute.HeaderName, "1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        using (JsonDocument page = await GetJsonAsync(client, $"/api/todo-lists/{listId}/todos", ct))
        {
            Assert.Equal(20, page.RootElement.GetProperty("items").GetArrayLength());
            Assert.Equal(21, page.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Equal(2, page.RootElement.GetProperty("totalPages").GetInt32());
        }
        using (JsonDocument page = await GetJsonAsync(client, $"/api/todo-lists/{listId}/todos?pageNumber=2&pageSize=20", ct))
            Assert.Equal("Task 21", page.RootElement.GetProperty("items")[0].GetProperty("title").GetString());
        using (JsonDocument page = await GetJsonAsync(client, "/api/todo-lists?pageNumber=2&pageSize=1", ct))
        {
            Assert.Equal(1, page.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Equal(0, page.RootElement.GetProperty("items").GetArrayLength());
        }
        foreach (string query in new[] { "pageNumber=0", "pageSize=0", "pageSize=101", "pageNumber=2147483647&pageSize=100", "pageNumber=abc" })
        {
            using HttpResponseMessage response = await client.GetAsync("/api/todo-lists?" + query, ct);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using (HttpResponseMessage hidden = await client.GetAsync($"/api/todo-lists/{foreignListId}/todos", ct))
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using (HttpResponseMessage missing = await client.GetAsync("/api/todo-lists/2147483647/todos", ct))
        using (HttpResponseMessage forbidden = await client.GetAsync($"/api/todo-lists/{foreignListId}/todos", ct))
        {
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
            using var missingBody = JsonDocument.Parse(await missing.Content.ReadAsStringAsync(ct));
            using var forbiddenBody = JsonDocument.Parse(await forbidden.Content.ReadAsStringAsync(ct));
            foreach (string field in new[] { "title", "detail", "type", "status" })
                Assert.Equal(missingBody.RootElement.GetProperty(field).GetRawText(), forbiddenBody.RootElement.GetProperty(field).GetRawText());
            Assert.False(forbiddenBody.RootElement.TryGetProperty("code", out _));
        }
        using (HttpResponseMessage duplicate = await client.PostAsJsonAsync($"/api/todo-lists/{listId}/todos", new { title = "Task 1" }, ct))
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using (HttpResponseMessage denied = await client.GetAsync("/api/admin/users", ct))
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        using (HttpResponseMessage invalid = await client.PostAsJsonAsync($"/api/todo-lists/{listId}/todos", new { title = "" }, ct))
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using (HttpResponseMessage invalidId = await client.GetAsync("/api/todo-lists/0/todos", ct))
            Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);
        using (JsonDocument unchanged = await GetJsonAsync(client, $"/api/todo-lists/{listId}/todos", ct))
            Assert.Equal(21, unchanged.RootElement.GetProperty("totalCount").GetInt32());

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using (JsonDocument todo = await GetJsonAsync(client, $"/api/todo-lists/{foreignListId}/todos/{foreignTodoId}", ct))
            Assert.Equal("Foreign task", todo.RootElement.GetProperty("title").GetString());
        using (JsonDocument todo = await GetJsonAsync(client, $"/api/todo-lists/{listId}", ct))
            Assert.Equal("Own list", todo.RootElement.GetProperty("name").GetString());
        using (JsonDocument users = await GetJsonAsync(client, "/api/admin/users?pageSize=1&pageNumber=2", ct))
        {
            Assert.Equal(2, users.RootElement.GetProperty("totalCount").GetInt32());
            Assert.Equal(1, users.RootElement.GetProperty("items").GetArrayLength());
            string json = users.RootElement.GetRawText();
            Assert.DoesNotContain("private-", json);
            Assert.DoesNotContain("passwordHash", json);
            Assert.DoesNotContain("googleSubjectId", json);
        }
    }

    private static async Task<JsonDocument> GetJsonAsync(HttpClient client, string path, CancellationToken ct)
    {
        using HttpResponseMessage response = await client.GetAsync(path, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
    }
}
