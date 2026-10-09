using System;
using Microsoft.EntityFrameworkCore;

namespace TodoApi.Infrastructure.Persistence;

public static class TodoDbContextConfiguration
{
    public const int MaximumRetryCount = 3;
    public static readonly TimeSpan MaximumRetryDelay = TimeSpan.FromSeconds(5);

    public static void UseTodoSqlServer(this DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlServer(connectionString, sqlServer =>
        {
            sqlServer.EnableRetryOnFailure(
                maxRetryCount: MaximumRetryCount,
                maxRetryDelay: MaximumRetryDelay,
                errorNumbersToAdd: null);
        });
    }
}
