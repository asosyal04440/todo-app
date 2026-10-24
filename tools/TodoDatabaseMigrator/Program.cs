using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TodoApi.Infrastructure.Persistence;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: TodoDatabaseMigrator <directory containing API appsettings.json>");
    return 2;
}

try
{
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Path.GetFullPath(args[0]))
        .AddJsonFile("appsettings.json", optional: false)
        .AddEnvironmentVariables()
        .Build();
    string? connectionString = configuration.GetConnectionString("TodoDatabase");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("The TodoDatabase connection string is missing.");
    }

    var options = new DbContextOptionsBuilder<TodoDbContext>();
    options.UseTodoSqlServer(connectionString);
    await using var context = new TodoDbContext(options.Options);
    await TodoSchemaMigrator.MigrateAsync(context);
    Console.WriteLine("Todo database migrations completed successfully.");
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
