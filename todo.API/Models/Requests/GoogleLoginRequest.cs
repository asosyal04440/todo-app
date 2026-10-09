namespace TodoApi.API.Models.Requests;

public sealed class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}
