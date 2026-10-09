using System;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Exceptions;

namespace TodoApi.Infrastructure.Persistence;

public static class DatabaseErrorTranslator
{
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int UniqueConstraintViolation = 2627;

    public static Exception Translate(DbUpdateException exception)
    {
        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (exception is DbUpdateConcurrencyException)
        {
            return new ConcurrencyConflictException(
                "The record was changed or deleted by another request.",
                exception);
        }

        if (exception.InnerException is SqlException sqlException && IsUniqueViolation(sqlException.Number))
        {
            return new ConflictException(
                "The record conflicts with an existing record.",
                exception);
        }

        return new DataAccessException(
            "The changes could not be saved to the database.",
            exception);
    }

    public static bool IsUniqueViolation(int sqlErrorNumber)
    {
        return sqlErrorNumber == DuplicateKeyInUniqueIndex || sqlErrorNumber == UniqueConstraintViolation;
    }
}
