using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public static class TodoSchemaMigrator
{
    public const string PreparationMigration = "AddNormalizedNameColumns";
    public const string EnforcementMigration = "EnforceNormalizedNameUniqueness";

    public static async Task MigrateAsync(TodoDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.Database.IsSqlServer())
        {
            throw new InvalidOperationException("The schema migrator requires SQL Server.");
        }

        IMigrator migrator = context.GetService<IMigrator>();
        IEnumerable<string> applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        if (applied.Any(id => id.EndsWith("_" + EnforcementMigration, StringComparison.Ordinal)))
        {
            await migrator.MigrateAsync(cancellationToken: cancellationToken);
            return;
        }

        await migrator.MigrateAsync(PreparationMigration, cancellationToken);

        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            DbConnection connection = context.Database.GetDbConnection();
            DbTransaction dbTransaction = transaction.GetDbTransaction();

            List<NameRow> lists = await ReadRowsAsync(connection, dbTransaction,
                "SELECT [Id], [OwnerUserId], [Name] FROM [TodoLists] WITH (TABLOCKX, HOLDLOCK)", cancellationToken);
            List<NameRow> todos = await ReadRowsAsync(connection, dbTransaction,
                "SELECT [Id], [TodoListId], [Title] FROM [TodoItems] WITH (TABLOCKX, HOLDLOCK)", cancellationToken);

            List<NameKey> listKeys = NormalizeRows(lists, TodoList.MaximumNormalizedNameLength, "TodoLists");
            List<NameKey> todoKeys = NormalizeRows(todos, TodoItem.MaximumNormalizedTitleLength, "TodoItems");

            await WriteKeysAsync(connection, dbTransaction,
                "UPDATE [TodoLists] SET [NormalizedName] = @key WHERE [Id] = @id", listKeys, cancellationToken);
            await WriteKeysAsync(connection, dbTransaction,
                "UPDATE [TodoItems] SET [NormalizedTitle] = @key WHERE [Id] = @id", todoKeys, cancellationToken);

            await migrator.MigrateAsync(EnforcementMigration, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        await migrator.MigrateAsync(cancellationToken: cancellationToken);
    }

    private static async Task<List<NameRow>> ReadRowsAsync(
        DbConnection connection, DbTransaction transaction, string sql, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        var rows = new List<NameRow>();
        await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new NameRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));
        }

        return rows;
    }

    private static List<NameKey> NormalizeRows(List<NameRow> rows, int maximumLength, string table)
    {
        var seen = new Dictionary<(int ScopeId, string Key), int>();
        var keys = new List<NameKey>(rows.Count);
        foreach (NameRow row in rows)
        {
            string key = NameComparison.NormalizeForLookup(row.Name);
            if (key.Length == 0 || key.Length > maximumLength)
            {
                throw new InvalidOperationException($"{table} record {row.Id} has an invalid normalized name.");
            }

            if (!seen.TryAdd((row.ScopeId, key), row.Id))
            {
                int existingId = seen[(row.ScopeId, key)];
                throw new InvalidOperationException(
                    $"Name normalization conflict in {table}: records {existingId} and {row.Id}. " +
                    "Resolve their names and run the migrator again. No records were deleted.");
            }

            keys.Add(new NameKey(row.Id, key));
        }

        return keys;
    }

    private static async Task WriteKeysAsync(DbConnection connection, DbTransaction transaction,
        string sql, List<NameKey> keys, CancellationToken cancellationToken)
    {
        await using DbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        DbParameter idParameter = command.CreateParameter();
        idParameter.ParameterName = "@id";
        idParameter.DbType = DbType.Int32;
        command.Parameters.Add(idParameter);
        DbParameter keyParameter = command.CreateParameter();
        keyParameter.ParameterName = "@key";
        keyParameter.DbType = DbType.String;
        keyParameter.Size = Math.Max(TodoList.MaximumNormalizedNameLength, TodoItem.MaximumNormalizedTitleLength);
        command.Parameters.Add(keyParameter);

        foreach (NameKey key in keys)
        {
            idParameter.Value = key.Id;
            keyParameter.Value = key.Key;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private sealed record NameRow(int Id, int ScopeId, string Name);
    private sealed record NameKey(int Id, string Key);
}
