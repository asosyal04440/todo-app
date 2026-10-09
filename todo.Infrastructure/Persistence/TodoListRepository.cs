using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public sealed class TodoListRepository : ITodoListRepository
{
    private readonly TodoDbContext _todoDbContext;

    public TodoListRepository(TodoDbContext todoDbContext)
    {
        if (todoDbContext == null)
        {
            throw new ArgumentNullException(nameof(todoDbContext));
        }

        _todoDbContext = todoDbContext;
    }

    public async Task<IReadOnlyList<TodoList>> GetAllByOwnerAsync(
        int ownerUserId,
        CancellationToken cancellationToken)
    {
        IQueryable<TodoList> todoListQuery = _todoDbContext.TodoLists;

        todoListQuery = todoListQuery.AsNoTracking();
        todoListQuery = todoListQuery.Where(todoList => todoList.OwnerUserId == ownerUserId);
        todoListQuery = todoListQuery.OrderBy(todoList => todoList.Id);

        List<TodoList> todoLists = await todoListQuery.ToListAsync(cancellationToken);

        IReadOnlyList<TodoList> result = todoLists;

        return result;
    }

    public async Task<TodoList?> GetByIdWithTodosAsync(
        int todoListId,
        CancellationToken cancellationToken)
    {
        IQueryable<TodoList> todoListQuery = _todoDbContext.TodoLists;

        todoListQuery = todoListQuery.Include(todoList => todoList.Todos);

        TodoList? todoList = await todoListQuery.FirstOrDefaultAsync(
            todoList => todoList.Id == todoListId,
            cancellationToken);

        return todoList;
    }

    public async Task<bool> ExistsWithNameForOwnerAsync(
        int ownerUserId,
        string name,
        int? excludedTodoListId,
        CancellationToken cancellationToken)
    {
        IQueryable<TodoList> todoListQuery = _todoDbContext.TodoLists;

        todoListQuery = todoListQuery.Where(todoList =>
            todoList.OwnerUserId == ownerUserId && todoList.Name == name);

        if (excludedTodoListId.HasValue)
        {
            int excludedId = excludedTodoListId.Value;
            todoListQuery = todoListQuery.Where(todoList => todoList.Id != excludedId);
        }

        bool exists = await todoListQuery.AnyAsync(cancellationToken);

        return exists;
    }

    public async Task AddAsync(
        TodoList todoList,
        CancellationToken cancellationToken)
    {
        if (todoList == null)
        {
            throw new ArgumentNullException(nameof(todoList));
        }

        await _todoDbContext.TodoLists.AddAsync(todoList, cancellationToken);
    }

    public void Delete(TodoList todoList)
    {
        if (todoList == null)
        {
            throw new ArgumentNullException(nameof(todoList));
        }

        _todoDbContext.TodoLists.Remove(todoList);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _todoDbContext.SaveChangesAsync(cancellationToken);
    }
}
