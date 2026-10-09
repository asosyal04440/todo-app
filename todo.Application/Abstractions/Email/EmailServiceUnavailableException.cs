using System;

namespace TodoApi.Application.Abstractions.Email;

public sealed class EmailServiceUnavailableException : Exception
{
    public EmailServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
