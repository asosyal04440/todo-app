namespace TodoApi.Application.Features.Authentication.LoginWithGoogle;

public sealed class LoginWithGoogleCommand
{
    public string IdToken { get; init; } = string.Empty;
}
