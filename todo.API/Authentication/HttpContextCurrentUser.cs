using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Enums;

namespace TodoApi.API.Authentication;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        if (httpContextAccessor == null)
        {
            throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            ClaimsPrincipal principal = GetAuthenticatedPrincipal();
            string? subjectClaimValue = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            bool isValidUserId = int.TryParse(subjectClaimValue, out int userId);

            if (isValidUserId == false)
            {
                throw new InvalidOperationException(
                    "The access token does not contain a valid user id.");
            }

            return userId;
        }
    }

    public bool IsAdmin
    {
        get
        {
            ClaimsPrincipal principal = GetAuthenticatedPrincipal();

            return principal.IsInRole(UserRole.Admin.ToString());
        }
    }

    private ClaimsPrincipal GetAuthenticatedPrincipal()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;

        if (httpContext == null || httpContext.User.Identity?.IsAuthenticated != true)
        {
            throw new InvalidOperationException(
                "The current user was requested outside of an authenticated request.");
        }

        return httpContext.User;
    }
}
