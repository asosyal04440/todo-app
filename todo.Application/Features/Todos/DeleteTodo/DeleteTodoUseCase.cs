using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.DeleteTodo;

public class DeleteTodoUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public DeleteTodoUseCase(
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

    public async Task<bool> ExecuteAsync(
        int todoListId,
        int todoId,
        CancellationToken cancellationToken = default)
    {
        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            todoListId,
            cancellationToken);

        if (todoList == null)
        {
            return false;
        }

        bool canAccessTodoList = TodoListAccessPolicy.CanAccess(todoList, _currentUser);

        if (canAccessTodoList == false)
        {
            return false;
        }

        bool todoWasRemoved = todoList.RemoveTodo(todoId);

        if (todoWasRemoved == false)
        {
            return false;
        }

        await _todoListRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
