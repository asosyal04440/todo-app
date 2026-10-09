namespace TodoApi.API.Models.Requests;

public sealed class CreateTodoListRequest
{
    public string Name { get; set; } = string.Empty;
}
