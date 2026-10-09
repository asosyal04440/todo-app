namespace TodoApi.API.Models.Requests;

public sealed class RegisterRequest
{
    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
