using System;

namespace TodoApi.Application.Exceptions;

public sealed class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
