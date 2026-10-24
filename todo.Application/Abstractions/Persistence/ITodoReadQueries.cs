using TodoApi.Application.Contracts;
using TodoApi.Application.Common.Results;

namespace TodoApi.Application.Abstractions.Persistence;

public interface ITodoReadQueries
{
    Task<PagedResult<TodoListDto>> GetListsAsync(int ownerUserId, PageRequest page, CancellationToken cancellationToken);
    Task<Result<TodoListDto>> GetListAsync(int listId, int userId, bool isAdmin, CancellationToken cancellationToken);
    Task<Result<PagedResult<TodoItemDto>>> GetTodosAsync(int listId, int userId, bool isAdmin, PageRequest page, CancellationToken cancellationToken);
    Task<Result<TodoItemDto>> GetTodoAsync(int listId, int todoId, int userId, bool isAdmin, CancellationToken cancellationToken);
}
