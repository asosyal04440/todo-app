using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Abstractions.Persistence;

public interface ITodoListRepository
{
    Task<IReadOnlyList<TodoList>> GetAllByOwnerAsync(
        int ownerUserId,
        CancellationToken cancellationToken);

    Task<TodoList?> GetByIdWithTodosAsync(int todoListId, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameForOwnerAsync(
        int ownerUserId,
        string name,
        int? excludedTodoListId,
        CancellationToken cancellationToken);

    Task AddAsync(TodoList todoList, CancellationToken cancellationToken);

    void Delete(TodoList todoList);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
