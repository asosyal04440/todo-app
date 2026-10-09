using System;

namespace TodoApi.Application.Exceptions;

public sealed class DataAccessException : Exception
{
    public DataAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
