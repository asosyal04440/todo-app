namespace TodoApi.Application.Features.Authentication.LoginWithPassword;

public sealed class LoginWithPasswordCommand
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
