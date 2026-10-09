using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.TodoLists.GetTodoListById;

public class GetTodoListByIdUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public GetTodoListByIdUseCase(
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

    public async Task<TodoListDto?> ExecuteAsync(
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

        TodoListDto response = new TodoListDto(todoList);

        return response;
    }
}
