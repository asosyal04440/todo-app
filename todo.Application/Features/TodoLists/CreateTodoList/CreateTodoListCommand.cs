namespace TodoApi.Application.Features.TodoLists.CreateTodoList;

public sealed class CreateTodoListCommand
{
    public string Name { get; init; } = string.Empty;
}
