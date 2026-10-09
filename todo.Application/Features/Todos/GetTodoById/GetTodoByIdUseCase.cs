using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.GetTodoById;

public class GetTodoByIdUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public GetTodoByIdUseCase(
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

    public async Task<TodoItemDto?> ExecuteAsync(
        int todoListId,
        int todoId,
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

        TodoItem? requestedTodo = null;

        foreach (TodoItem todoItem in todoList.Todos)
        {
            if (todoItem.Id == todoId)
            {
                requestedTodo = todoItem;
                break;
            }
        }

        if (requestedTodo == null)
        {
            return null;
        }

        TodoItemDto response = new TodoItemDto(requestedTodo);

        return response;
    }
}
