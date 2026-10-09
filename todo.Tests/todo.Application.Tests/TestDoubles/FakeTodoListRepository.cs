using System.Reflection;
using TodoApi.Domain.Common;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeTodoListRepository : ITodoListRepository
{
    private readonly List<TodoList> _todoLists = new List<TodoList>();
    private int _nextTodoListId = 1;
    private int _nextTodoItemId = 1;

    public int SaveChangesCount { get; private set; }

    public IReadOnlyList<TodoList> TodoLists
    {
        get
        {
            return _todoLists;
        }
    }

    public TodoList AddExistingTodoList(
        string name,
        int ownerUserId = FakeCurrentUser.ListOwnerUserId)
    {
        TodoList todoList = new TodoList(name, ownerUserId);
        SetPrivateProperty(todoList, nameof(TodoList.Id), _nextTodoListId);
        _nextTodoListId++;
        _todoLists.Add(todoList);

        return todoList;
    }

    public TodoItem AddExistingTodoItem(TodoList todoList, string title)
    {
        TodoItem todoItem = todoList.AddTodo(title);
        SetPrivateProperty(todoItem, nameof(TodoItem.Id), _nextTodoItemId);
        SetPrivateProperty(todoItem, nameof(TodoItem.TodoListId), todoList.Id);
        _nextTodoItemId++;

        return todoItem;
    }

    public Task<IReadOnlyList<TodoList>> GetAllByOwnerAsync(
        int ownerUserId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TodoList> results = _todoLists
            .Where(todoList => todoList.OwnerUserId == ownerUserId)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<TodoList?> GetByIdWithTodosAsync(
        int todoListId,
        CancellationToken cancellationToken)
    {
        TodoList? todoList = _todoLists.Find(
            existingTodoList => existingTodoList.Id == todoListId);

        return Task.FromResult(todoList);
    }

    public Task<bool> ExistsWithNameForOwnerAsync(
        int ownerUserId,
        string name,
        int? excludedTodoListId,
        CancellationToken cancellationToken)
    {
        bool exists = _todoLists.Any(todoList =>
            todoList.OwnerUserId == ownerUserId &&
            todoList.Id != excludedTodoListId &&
            NameComparison.AreSame(todoList.Name, name));

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        TodoList todoList,
        CancellationToken cancellationToken)
    {
        _todoLists.Add(todoList);

        return Task.CompletedTask;
    }

    public void Delete(TodoList todoList)
    {
        _todoLists.Remove(todoList);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (TodoList todoList in _todoLists)
        {
            if (todoList.Id == 0)
            {
                SetPrivateProperty(todoList, nameof(TodoList.Id), _nextTodoListId);
                _nextTodoListId++;
            }

            foreach (TodoItem todoItem in todoList.Todos)
            {
                if (todoItem.Id == 0)
                {
                    SetPrivateProperty(todoItem, nameof(TodoItem.Id), _nextTodoItemId);
                    _nextTodoItemId++;
                }

                if (todoItem.TodoListId != todoList.Id)
                {
                    SetPrivateProperty(
                        todoItem,
                        nameof(TodoItem.TodoListId),
                        todoList.Id);
                }
            }
        }

        SaveChangesCount++;

        return Task.CompletedTask;
    }

    private static void SetPrivateProperty<T>(
        T instance,
        string propertyName,
        int value)
    {
        PropertyInfo property = typeof(T).GetProperty(propertyName)
            ?? throw new InvalidOperationException(
                $"The {propertyName} property was not found.");

        MethodInfo setter = property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException(
                $"The {propertyName} property does not have a setter.");

        setter.Invoke(instance, new object[] { value });
    }
}
