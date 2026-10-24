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
using TodoApi.Infrastructure.Persistence;

namespace todo.API.Tests;

public class UnitOfWorkApiTests
{
    [Fact]
    public async Task RegisterLoginAndCreateTodo_PersistThroughTheSharedScopedUnitOfWork()
    {
        using SqliteConnection connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<TodoDbContext>().UseSqlite(connection).Options;
        using TodoApiFactory originalFactory = new TodoApiFactory();
        using WebApplicationFactory<Program> factory = originalFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TodoDbContext>();
                services.RemoveAll<DbContextOptions<TodoDbContext>>();
                services.AddSingleton(options);
                services.AddScoped<TodoDbContext>();
            }));

        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<TodoDbContext>().Database
                .EnsureCreatedAsync(TestContext.Current.CancellationToken);
        }

        using HttpClient client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add(RequireClientHeaderAttribute.HeaderName, "1");
        using HttpResponseMessage registration = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "unit-of-work@example.com", displayName = "Test user", password = "Sifre1234" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        using JsonDocument registered = JsonDocument.Parse(await registration.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        int userId = registered.RootElement.GetProperty("user").GetProperty("id").GetInt32();
        Assert.True(userId > 0);
        Assert.Contains(registration.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith("todo_refresh=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        using (HttpResponseMessage duplicateRegistration = await client.PostAsJsonAsync("/api/auth/register",
            new { email = "unit-of-work@example.com", displayName = "Duplicate", password = "Sifre1234" },
            TestContext.Current.CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicateRegistration.StatusCode);
            Assert.False(duplicateRegistration.Headers.Contains("Set-Cookie"));
        }
        using (HttpResponseMessage wrongPassword = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "unit-of-work@example.com", password = "Yanlis1234" }, TestContext.Current.CancellationToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
            Assert.False(wrongPassword.Headers.Contains("Set-Cookie"));
        }
        using (HttpClient anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), HandleCookies = false
        }))
        {
            anonymous.DefaultRequestHeaders.Add(RequireClientHeaderAttribute.HeaderName, "1");
            using HttpResponseMessage invalidSession = await anonymous.PostAsync("/api/auth/refresh", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, invalidSession.StatusCode);
            Assert.Contains(invalidSession.Headers.GetValues("Set-Cookie"), value => value.StartsWith("todo_refresh=;")
                && value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        }

        using HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "unit-of-work@example.com", password = "Sifre1234" },
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using JsonDocument signedIn = JsonDocument.Parse(await login.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        Assert.Equal(userId, signedIn.RootElement.GetProperty("user").GetProperty("id").GetInt32());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", signedIn.RootElement.GetProperty("accessToken").GetString());

        using HttpResponseMessage createdList = await client.PostAsJsonAsync("/api/todo-lists",
            new { name = "Transaction lesson" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createdList.StatusCode);
        using JsonDocument list = JsonDocument.Parse(await createdList.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken));
        int listId = list.RootElement.GetProperty("id").GetInt32();

        using HttpResponseMessage createdTodo = await client.PostAsJsonAsync($"/api/todo-lists/{listId}/todos",
            new { title = "Learn Unit of Work" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, createdTodo.StatusCode);

        await using AsyncServiceScope verificationScope = factory.Services.CreateAsyncScope();
        TodoDbContext verification = verificationScope.ServiceProvider.GetRequiredService<TodoDbContext>();
        Assert.Equal(1, await verification.Users.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await verification.RefreshTokens.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(userId, (await verification.TodoLists.SingleAsync(
            TestContext.Current.CancellationToken)).OwnerUserId);
        Assert.Equal(listId, (await verification.TodoItems.SingleAsync(
            TestContext.Current.CancellationToken)).TodoListId);
    }
}
