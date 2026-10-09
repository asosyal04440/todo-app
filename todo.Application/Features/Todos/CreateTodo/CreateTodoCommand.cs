using System;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Features.Todos.CreateTodo;

public sealed class CreateTodoCommand
{
    public int TodoListId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DateTime? DueAtUtc { get; init; }

    public TodoPriority Priority { get; init; } = TodoPriority.Normal;
}
