using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public sealed class TodoReminderRepository : ITodoReminderRepository
{
    private readonly TodoDbContext _todoDbContext;

    public TodoReminderRepository(TodoDbContext todoDbContext)
    {
        _todoDbContext = todoDbContext ?? throw new ArgumentNullException(nameof(todoDbContext));
    }

    public async Task<IReadOnlyList<TodoReminderCandidate>> GetTodosDueForReminderAsync(
        DateTime nowUtc,
        DateTime dueBeforeUtc,
        DateTime lastFailedAttemptBeforeUtc,
        int maximumAttempts,
        int maxCount,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from todo in _todoDbContext.TodoItems
            join todoList in _todoDbContext.TodoLists on todo.TodoListId equals todoList.Id
            join owner in _todoDbContext.Users on todoList.OwnerUserId equals owner.Id
            where todo.IsCompleted == false
                && todo.ReminderSentAtUtc == null
                && todo.DueAtUtc != null
                && todo.DueAtUtc > nowUtc
                && todo.DueAtUtc <= dueBeforeUtc
                && todo.ReminderAttemptCount < maximumAttempts
                && (todo.ReminderLastAttemptAtUtc == null || todo.ReminderLastAttemptAtUtc <= lastFailedAttemptBeforeUtc)
            orderby todo.DueAtUtc
            select new
            {
                Todo = todo,
                ListName = todoList.Name,
                OwnerUserId = owner.Id,
                OwnerEmail = owner.Email,
                OwnerDisplayName = owner.DisplayName
            })
            .Take(maxCount)
            .ToListAsync(cancellationToken);

        List<TodoReminderCandidate> candidates = new List<TodoReminderCandidate>();

        foreach (var row in rows)
        {
            candidates.Add(new TodoReminderCandidate(
                row.Todo,
                row.ListName,
                row.OwnerUserId,
                row.OwnerEmail,
                row.OwnerDisplayName));
        }

        return candidates;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _todoDbContext.SaveChangesAsync(cancellationToken);
    }
}
