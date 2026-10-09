using System;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Contracts;

public class TodoListDto
{
    private readonly int _id;
    private readonly string _name;
    private readonly DateTime _createdAtUtc;

    public TodoListDto(TodoList todoList)
    {
        if (todoList == null)
        {
            throw new ArgumentNullException(nameof(todoList));
        }

        _id = todoList.Id;
        _name = todoList.Name;
        _createdAtUtc = todoList.CreatedAtUtc;
    }

    public int Id
    {
        get
        {
            return _id;
        }
    }

    public string Name
    {
        get
        {
            return _name;
        }
    }

    public DateTime CreatedAtUtc
    {
        get
        {
            return _createdAtUtc;
        }
    }
}
