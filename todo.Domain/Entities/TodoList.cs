using TodoApi.Domain.Common;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Domain.Entities;

public class TodoList
{
    private const int MaximumNameLength = 80;

    private int _id;
    private string _name = string.Empty;
    private int _ownerUserId;
    private DateTime _createdAtUtc;
    private readonly List<TodoItem> _todos = new List<TodoItem>();

    public TodoList(string name, int ownerUserId)
    {
        string normalizedName = ValidateAndNormalizeName(name);
        Name = normalizedName;
        OwnerUserId = ValidateOwnerUserId(ownerUserId);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public int Id
    {
        get
        {
            return _id;
        }
        private set
        {
            _id = value;
        }
    }

    public string Name
    {
        get
        {
            return _name;
        }
        private set
        {
            _name = value;
        }
    }

    public int OwnerUserId
    {
        get
        {
            return _ownerUserId;
        }
        private set
        {
            _ownerUserId = value;
        }
    }

    public DateTime CreatedAtUtc
    {
        get
        {
            return _createdAtUtc;
        }
        private set
        {
            _createdAtUtc = value;
        }
    }

    public IReadOnlyCollection<TodoItem> Todos
    {
        get
        {
            return _todos.AsReadOnly();
        }
    }

    public bool IsOwnedBy(int userId)
    {
        return OwnerUserId == userId;
    }

    public void Rename(string name)
    {
        string normalizedName = ValidateAndNormalizeName(name);
        Name = normalizedName;
    }

    public TodoItem AddTodo(
        string title,
        string? description = null,
        DateTime? dueAtUtc = null,
        TodoPriority priority = TodoPriority.Normal)
    {
        TodoItem todo = new TodoItem(
            Id,
            title,
            description,
            dueAtUtc,
            priority);

        EnsureTitleIsUnique(todo.Title, todoBeingRenamed: null);

        _todos.Add(todo);

        return todo;
    }

    public void RenameTodo(TodoItem todo, string title)
    {
        if (todo == null)
        {
            throw new ArgumentNullException(nameof(todo));
        }

        if (_todos.Contains(todo) == false)
        {
            throw new DomainRuleViolationException(
                "A todo can only be renamed through the list it belongs to.");
        }

        EnsureTitleIsUnique(title, todoBeingRenamed: todo);

        todo.Rename(title);
    }

    public bool RemoveTodo(int todoId)
    {
        if (todoId <= 0)
        {
            throw new DomainRuleViolationException(
                "A saved todo must have a positive id before it can be removed by id.");
        }

        TodoItem? todoToRemove = null;

        foreach (TodoItem todo in _todos)
        {
            if (todo.Id == todoId)
            {
                todoToRemove = todo;
                break;
            }
        }

        if (todoToRemove == null)
        {
            return false;
        }

        bool wasRemoved = _todos.Remove(todoToRemove);

        return wasRemoved;
    }

    private void EnsureTitleIsUnique(string title, TodoItem? todoBeingRenamed)
    {
        foreach (TodoItem existingTodo in _todos)
        {
            if (ReferenceEquals(existingTodo, todoBeingRenamed))
            {
                continue;
            }

            if (NameComparison.AreSame(existingTodo.Title, title))
            {
                throw new DuplicateNameException(
                    "A todo with the same title already exists in this list.");
            }
        }
    }

    private static string ValidateAndNormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainRuleViolationException(
                "A todo list name cannot be empty.");
        }

        string normalizedName = name.Trim();

        if (normalizedName.Length > MaximumNameLength)
        {
            throw new DomainRuleViolationException(
                $"A todo list name cannot exceed {MaximumNameLength} characters.");
        }

        return normalizedName;
    }

    private static int ValidateOwnerUserId(int ownerUserId)
    {
        if (ownerUserId <= 0)
        {
            throw new DomainRuleViolationException(
                "A todo list must belong to a saved user.");
        }

        return ownerUserId;
    }
}
