using System;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;

namespace TodoApi.Application.Contracts;

public class TodoItemDto
{
    private readonly int _id;
    private readonly int _todoListId;
    private readonly string _title;
    private readonly string? _description;
    private readonly bool _isCompleted;
    private readonly DateTime _createdAtUtc;
    private readonly DateTime? _dueAtUtc;
    private readonly TodoPriority _priority;

    public TodoItemDto(TodoItem todoItem)
    {
        if (todoItem == null)
        {
            throw new ArgumentNullException(nameof(todoItem));
        }

        _id = todoItem.Id;
        _todoListId = todoItem.TodoListId;
        _title = todoItem.Title;
        _description = todoItem.Description;
        _isCompleted = todoItem.IsCompleted;
        _createdAtUtc = todoItem.CreatedAtUtc;
        _dueAtUtc = todoItem.DueAtUtc;
        _priority = todoItem.Priority;
    }

    public TodoItemDto(int id, int todoListId, string title, string? description,
        bool isCompleted, DateTime createdAtUtc, DateTime? dueAtUtc, TodoPriority priority)
    {
        _id = id;
        _todoListId = todoListId;
        _title = title;
        _description = description;
        _isCompleted = isCompleted;
        _createdAtUtc = createdAtUtc;
        _dueAtUtc = dueAtUtc;
        _priority = priority;
    }

    public int Id
    {
        get
        {
            return _id;
        }
    }

    public int TodoListId
    {
        get
        {
            return _todoListId;
        }
    }

    public string Title
    {
        get
        {
            return _title;
        }
    }

    public string? Description
    {
        get
        {
            return _description;
        }
    }

    public bool IsCompleted
    {
        get
        {
            return _isCompleted;
        }
    }

    public DateTime CreatedAtUtc
    {
        get
        {
            return _createdAtUtc;
        }
    }

    public DateTime? DueAtUtc
    {
        get
        {
            return _dueAtUtc;
        }
    }

    public TodoPriority Priority
    {
        get
        {
            return _priority;
        }
    }
}
