using Microsoft.Extensions.Options;
using TodoApi.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TodoApi.API.Authentication;
using TodoApi.API.Models.Requests;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Contracts;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Authentication.GetCurrentUser;
using TodoApi.Application.Features.Authentication.LoginWithGoogle;
using TodoApi.Application.Features.Authentication.LoginWithPassword;
using TodoApi.Application.Features.Authentication.Logout;
using TodoApi.Application.Features.Authentication.RefreshSession;
using TodoApi.Application.Features.Authentication.RegisterWithPassword;
using TodoApi.Infrastructure.Services.Authentication;

namespace TodoApi.API.Controllers;

[ApiController]
[Route("api/auth")]
[RequireClientHeader]
public sealed class AuthController : ControllerBase
{
    private readonly RegisterWithPasswordUseCase _registerWithPasswordUseCase;
    private readonly LoginWithPasswordUseCase _loginWithPasswordUseCase;
    private readonly LoginWithGoogleUseCase _loginWithGoogleUseCase;
    private readonly RefreshSessionUseCase _refreshSessionUseCase;
    private readonly LogoutUseCase _logoutUseCase;
    private readonly GetCurrentUserUseCase _getCurrentUserUseCase;
    private readonly GoogleAuthenticationSettings _googleSettings;

    public AuthController(
        RegisterWithPasswordUseCase registerWithPasswordUseCase,
        LoginWithPasswordUseCase loginWithPasswordUseCase,
        LoginWithGoogleUseCase loginWithGoogleUseCase,
        RefreshSessionUseCase refreshSessionUseCase,
        LogoutUseCase logoutUseCase,
        GetCurrentUserUseCase getCurrentUserUseCase,
        IOptions<GoogleAuthenticationOptions> googleSettings)
    {
        _registerWithPasswordUseCase = registerWithPasswordUseCase;
        _loginWithPasswordUseCase = loginWithPasswordUseCase;
        _loginWithGoogleUseCase = loginWithGoogleUseCase;
        _refreshSessionUseCase = refreshSessionUseCase;
        _logoutUseCase = logoutUseCase;
        _getCurrentUserUseCase = getCurrentUserUseCase;
        _googleSettings = googleSettings.Value.ToSettings();
    }

    [AllowAnonymous]
    [HttpGet("config")]
    public ActionResult<AuthConfigResponse> GetConfig()
    {
        AuthConfigResponse response = new AuthConfigResponse
        {
            GoogleClientId = _googleSettings.ClientId
        };

        return Ok(response);
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.Authentication)]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        RegisterWithPasswordCommand command = new RegisterWithPasswordCommand
        {
            Email = request.Email, DisplayName = request.DisplayName, Password = request.Password
        };
        var result = await _registerWithPasswordUseCase.ExecuteAsync(command, cancellationToken);
        return result.ToActionResult(this, session =>
        {
            RefreshTokenCookie.Write(Response, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            return CreatedAtAction(nameof(GetCurrentUser), null, session.Result);
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.Authentication)]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginWithPasswordCommand { Email = request.Email, Password = request.Password };
        var result = await _loginWithPasswordUseCase.ExecuteAsync(command, cancellationToken);
        return result.ToActionResult(this, session =>
        {
            RefreshTokenCookie.Write(Response, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            return Ok(session.Result);
        });
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.Authentication)]
    [HttpPost("google")]
    public async Task<ActionResult<AuthResultDto>> LoginWithGoogle(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginWithGoogleCommand { IdToken = request.IdToken };
        var result = await _loginWithGoogleUseCase.ExecuteAsync(command, cancellationToken);
        return result.ToActionResult(this, session =>
        {
            RefreshTokenCookie.Write(Response, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            return Ok(session.Result);
        });
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> Refresh(CancellationToken cancellationToken)
    {
        string? refreshToken = RefreshTokenCookie.Read(Request);
        var result = await _refreshSessionUseCase.ExecuteAsync(refreshToken, cancellationToken);
        if (result.Error?.Kind == ErrorKind.Unauthorized)
            RefreshTokenCookie.Delete(Response);
        return result.ToActionResult(this, session =>
        {
            RefreshTokenCookie.Write(Response, session.RefreshToken, session.RefreshTokenExpiresAtUtc);
            return Ok(session.Result);
        });
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await _logoutUseCase.ExecuteAsync(RefreshTokenCookie.Read(Request), cancellationToken);
        return result.ToActionResult(this, _ =>
        {
            RefreshTokenCookie.Delete(Response);
            return NoContent();
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        return (await _getCurrentUserUseCase.ExecuteAsync(cancellationToken)).ToActionResult(this);
    }
}
