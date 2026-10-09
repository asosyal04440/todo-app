namespace TodoApi.Application.Features.Authentication.Common;

public static class EmailNormalizer
{
    public static string Normalize(string? email)
    {
        if (email == null)
        {
            return string.Empty;
        }

        return email.Trim().ToLowerInvariant();
    }
}
