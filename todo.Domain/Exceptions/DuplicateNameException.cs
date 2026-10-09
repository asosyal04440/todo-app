namespace TodoApi.Domain.Exceptions;

public sealed class DuplicateNameException : Exception
{
    public DuplicateNameException(string message) : base(message)
    {
    }
}
