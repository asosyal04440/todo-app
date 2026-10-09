using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.GetTodosInList;

public class GetTodosInListUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public GetTodosInListUseCase(
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser)
    {
        if (todoListRepository == null)
        {
            throw new ArgumentNullException(nameof(todoListRepository));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        _todoListRepository = todoListRepository;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TodoItemDto>?> ExecuteAsync(
        int todoListId,
        CancellationToken cancellationToken = default)
    {
        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            todoListId,
            cancellationToken);

        if (todoList == null)
        {
            return null;
        }

        bool canAccessTodoList = TodoListAccessPolicy.CanAccess(todoList, _currentUser);

        if (canAccessTodoList == false)
        {
            return null;
        }

        List<TodoItemDto> responses = new List<TodoItemDto>();

        foreach (TodoItem todoItem in todoList.Todos)
        {
            TodoItemDto response = new TodoItemDto(todoItem);
            responses.Add(response);
        }

        return responses;
    }
}
