using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.TodoLists.GetTodoLists;

public class GetTodoListsUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public GetTodoListsUseCase(
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

    public async Task<IReadOnlyList<TodoListDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TodoList> todoLists = await _todoListRepository.GetAllByOwnerAsync(
            _currentUser.UserId,
            cancellationToken);

        List<TodoListDto> responses = new List<TodoListDto>();

        foreach (TodoList todoList in todoLists)
        {
            TodoListDto response = new TodoListDto(todoList);
            responses.Add(response);
        }

        return responses;
    }
}
