namespace TodoApi.Application.Features.TodoLists.RenameTodoList;

public sealed class RenameTodoListCommand
{
    public int TodoListId { get; init; }

    public string Name { get; init; } = string.Empty;
}
