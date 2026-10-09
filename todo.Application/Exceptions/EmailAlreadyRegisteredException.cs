using System;

namespace TodoApi.Application.Exceptions;

public sealed class EmailAlreadyRegisteredException : Exception
{
    public EmailAlreadyRegisteredException()
        : base("An account with this email address already exists.")
    {
    }
}
