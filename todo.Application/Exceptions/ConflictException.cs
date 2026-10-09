using System;

namespace TodoApi.Application.Exceptions;

public sealed class ConflictException : Exception
{
    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
