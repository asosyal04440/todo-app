using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.TodoLists.DeleteTodoList;

public class DeleteTodoListUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public DeleteTodoListUseCase(
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

        _todoListRepository.Delete(todoList);

        await _todoListRepository.SaveChangesAsync(cancellationToken);

        return true;
    }
}
