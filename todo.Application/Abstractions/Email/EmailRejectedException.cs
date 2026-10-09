using System;

namespace TodoApi.Application.Abstractions.Email;

public sealed class EmailRejectedException : Exception
{
    public EmailRejectedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
