using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Abstractions.Persistence;

public interface ITodoReminderRepository
{
    Task<IReadOnlyList<TodoReminderCandidate>> GetTodosDueForReminderAsync(
        DateTime nowUtc,
        DateTime dueBeforeUtc,
        DateTime lastFailedAttemptBeforeUtc,
        int maximumAttempts,
        int maxCount,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public sealed class TodoReminderCandidate
{
    private readonly TodoItem _todo;
    private readonly string _listName;
    private readonly int _ownerUserId;
    private readonly string _ownerEmail;
    private readonly string _ownerDisplayName;

    public TodoReminderCandidate(
        TodoItem todo,
        string listName,
        int ownerUserId,
        string ownerEmail,
        string ownerDisplayName)
    {
        _todo = todo ?? throw new ArgumentNullException(nameof(todo));
        _listName = listName ?? string.Empty;
        _ownerUserId = ownerUserId;
        _ownerEmail = ownerEmail ?? throw new ArgumentNullException(nameof(ownerEmail));
        _ownerDisplayName = ownerDisplayName ?? string.Empty;
    }

    public TodoItem Todo
    {
        get
        {
            return _todo;
        }
    }

    public string ListName
    {
        get
        {
            return _listName;
        }
    }

    public int OwnerUserId
    {
        get
        {
            return _ownerUserId;
        }
    }

    public string OwnerEmail
    {
        get
        {
            return _ownerEmail;
        }
    }

    public string OwnerDisplayName
    {
        get
        {
            return _ownerDisplayName;
        }
    }
}
