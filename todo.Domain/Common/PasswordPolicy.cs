namespace TodoApi.Domain.Common;

public static class PasswordPolicy
{
    public const int MinimumLength = 8;
    public const int MaximumLength = 128;

    public static bool ContainsLetterAndDigit(string? password)
    {
        return password != null
            && password.Any(char.IsLetter)
            && password.Any(char.IsDigit);
    }
}
