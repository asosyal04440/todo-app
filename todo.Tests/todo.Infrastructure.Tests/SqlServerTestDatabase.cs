using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TodoApi.Infrastructure.Persistence;

namespace todo.Infrastructure.Tests;

internal sealed class SqlServerTestDatabase : IAsyncDisposable
{
    private readonly string _masterConnectionString;
    private readonly string _name;
    public string ConnectionString { get; }

    private SqlServerTestDatabase(string masterConnectionString, string name, string connectionString)
    {
        _masterConnectionString = masterConnectionString;
        _name = name;
        ConnectionString = connectionString;
    }

    public static async Task<SqlServerTestDatabase> CreateAsync()
    {
        string? configured = Environment.GetEnvironmentVariable("TODO_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Skip("Set TODO_TEST_SQLSERVER to run isolated SQL Server integration tests.");
        }

        var master = new SqlConnectionStringBuilder(configured) { InitialCatalog = "master", Pooling = false };
        string name = "TodoNameRules_" + Guid.NewGuid().ToString("N");
        await using SqlConnection connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new SqlCommand(
            $"CREATE DATABASE [{name}] COLLATE SQL_Latin1_General_CP1_CS_AS", connection);
        await command.ExecuteNonQueryAsync();
        var database = new SqlConnectionStringBuilder(master.ConnectionString) { InitialCatalog = name };
        return new SqlServerTestDatabase(master.ConnectionString, name, database.ConnectionString);
    }

    public TodoDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<TodoDbContext>();
        options.UseTodoSqlServer(ConnectionString);
        options.AddInterceptors(interceptors);
        return new TodoDbContext(options.Options);
    }

    public async ValueTask DisposeAsync()
    {
        if (!Regex.IsMatch(_name, "^TodoNameRules_[0-9a-f]{32}$"))
        {
            throw new InvalidOperationException("Refusing to drop an unexpected database name.");
        }

        await using SqlConnection connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();
        await using SqlCommand command = new SqlCommand(
            $"ALTER DATABASE [{_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_name}]", connection);
        await command.ExecuteNonQueryAsync();
    }
}
