using System;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Features.Todos.UpdateTodo;

public sealed class UpdateTodoCommand
{
    public int TodoListId { get; init; }

    public int TodoId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public DateTime? DueAtUtc { get; init; }

    public TodoPriority Priority { get; init; } = TodoPriority.Normal;
}
