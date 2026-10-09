namespace TodoApi.API.Authentication;

public static class RefreshTokenCookie
{
    public const string Name = "todo_refresh";

    public const string Path = "/api/auth";

    public static void Write(HttpResponse response, string refreshToken, DateTime expiresAtUtc)
    {
        response.Cookies.Append(Name, refreshToken, CreateOptions(expiresAtUtc));
    }

    public static string? Read(HttpRequest request)
    {
        bool cookieExists = request.Cookies.TryGetValue(Name, out string? refreshToken);

        if (cookieExists == false)
        {
            return null;
        }

        return refreshToken;
    }

    public static void Delete(HttpResponse response)
    {
        response.Cookies.Delete(Name, CreateOptions(DateTime.UnixEpoch));
    }

    private static CookieOptions CreateOptions(DateTime expiresAtUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = Path,
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc)),
            IsEssential = true
        };
    }
}
