using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Application.Common.Results;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public sealed class TodoReadQueries(TodoDbContext db) : ITodoReadQueries
{
    private static readonly Expression<Func<TodoList, TodoListDto>> ListProjection =
        list => new TodoListDto(list.Id, list.Name, list.CreatedAtUtc);

    private static readonly Expression<Func<TodoItem, TodoItemDto>> TodoProjection =
        todo => new TodoItemDto(todo.Id, todo.TodoListId, todo.Title, todo.Description,
            todo.IsCompleted, todo.CreatedAtUtc, todo.DueAtUtc, todo.Priority);

    public Task<PagedResult<TodoListDto>> GetListsAsync(int ownerUserId, PageRequest page, CancellationToken cancellationToken) =>
        ReadQueryPaging.ReadPageAsync(db.TodoLists.AsNoTracking()
            .Where(list => list.OwnerUserId == ownerUserId)
            .OrderBy(list => list.Id).Select(ListProjection), page, cancellationToken);

    public async Task<Result<TodoListDto>> GetListAsync(int listId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        TodoListDto? list = await AccessibleLists(listId, userId, isAdmin).Select(ListProjection).FirstOrDefaultAsync(cancellationToken);
        if (list != null) return Result<TodoListDto>.Success(list);
        return Result<TodoListDto>.Failure(await GetAccessErrorAsync(listId, userId, isAdmin, cancellationToken)
            ?? AppErrors.ListNotFound);
    }

    public async Task<Result<PagedResult<TodoItemDto>>> GetTodosAsync(
        int listId, int userId, bool isAdmin, PageRequest page, CancellationToken cancellationToken)
    {
    
        AppError? accessError = await GetAccessErrorAsync(listId, userId, isAdmin, cancellationToken);
        if (accessError != null) return Result<PagedResult<TodoItemDto>>.Failure(accessError);
        return Result<PagedResult<TodoItemDto>>.Success(await ReadQueryPaging.ReadPageAsync(AccessibleTodos(listId, userId, isAdmin)
            .OrderBy(todo => todo.Id).Select(TodoProjection), page, cancellationToken));
    }

    public async Task<Result<TodoItemDto>> GetTodoAsync(
        int listId, int todoId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        TodoItemDto? todo = await AccessibleTodos(listId, userId, isAdmin).Where(todo => todo.Id == todoId)
            .Select(TodoProjection).FirstOrDefaultAsync(cancellationToken);
        if (todo != null) return Result<TodoItemDto>.Success(todo);
        return Result<TodoItemDto>.Failure(await GetAccessErrorAsync(listId, userId, isAdmin, cancellationToken)
            ?? AppErrors.TodoNotFound);
    }

    private async Task<AppError?> GetAccessErrorAsync(int listId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        int? ownerId = await db.TodoLists.AsNoTracking().Where(list => list.Id == listId)
            .Select(list => (int?)list.OwnerUserId).FirstOrDefaultAsync(cancellationToken);
        if (ownerId == null) return AppErrors.ListNotFound;
        return isAdmin || ownerId == userId ? null : AppErrors.AccessDenied;
    }

    private IQueryable<TodoList> AccessibleLists(int listId, int userId, bool isAdmin) =>
        db.TodoLists.AsNoTracking().Where(list => list.Id == listId && (isAdmin || list.OwnerUserId == userId));

    private IQueryable<TodoItem> AccessibleTodos(int listId, int userId, bool isAdmin) =>
        from todo in db.TodoItems.AsNoTracking()
        join list in AccessibleLists(listId, userId, isAdmin) on todo.TodoListId equals list.Id
        select todo;
}
