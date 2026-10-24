using TodoApi.Domain.Common;

namespace todo.Domain.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("sadeceharf", false)]
    [InlineData("12345678", false)]
    [InlineData("Şifre123", true)]
    [InlineData("Şifre١٢٣", true)]
    [InlineData("𐐀1234567", false)]
    public void ContainsLetterAndDigit_UsesUnicodeCharacters(string? password, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.ContainsLetterAndDigit(password));
    }
}
