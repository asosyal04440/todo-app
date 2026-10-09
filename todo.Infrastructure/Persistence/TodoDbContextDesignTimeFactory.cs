using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TodoApi.Infrastructure.Persistence;

public sealed class TodoDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TodoDbContext>
{
    public TodoDbContext CreateDbContext(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        string? connectionString = configuration.GetConnectionString("TodoDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The TodoDatabase connection string is missing. Run the EF tools with " +
                "--startup-project todo.API or set the ConnectionStrings__TodoDatabase environment variable.");
        }

        DbContextOptionsBuilder<TodoDbContext> optionsBuilder = new DbContextOptionsBuilder<TodoDbContext>();
        optionsBuilder.UseTodoSqlServer(connectionString);

        return new TodoDbContext(optionsBuilder.Options);
    }
}
