namespace TodoApi.API.Models.Requests;

public sealed class RenameTodoListRequest
{
    public string Name { get; set; } = string.Empty;
}
