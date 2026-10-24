using System.Reflection;
using TodoApi.Domain.Common;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Application.Common.Results;
using TodoApi.Domain.Entities;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeTodoListRepository : ITodoListRepository, ITodoReadQueries
{
    public FakeTodoListRepository()
    {
        UnitOfWork = new FakeUnitOfWork(ApplyChanges);
    }

    public FakeUnitOfWork UnitOfWork { get; }

    private readonly List<TodoList> _todoLists = new List<TodoList>();
    private int _nextTodoListId = 1;
    private int _nextTodoItemId = 1;

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

    public Task<PagedResult<TodoListDto>> GetListsAsync(
        int ownerUserId,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var results = _todoLists
            .Where(todoList => todoList.OwnerUserId == ownerUserId)
            .OrderBy(todoList => todoList.Id)
            .ToList();

        return Task.FromResult(new PagedResult<TodoListDto>(results.Skip(page.Offset).Take(page.PageSize)
            .Select(list => new TodoListDto(list)).ToList(), results.Count, page));
    }

    public Task<Result<TodoListDto>> GetListAsync(int listId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        TodoList? list = _todoLists.FirstOrDefault(list => list.Id == listId);
        return Task.FromResult(list == null ? Result<TodoListDto>.Failure(AppErrors.ListNotFound)
            : !isAdmin && list.OwnerUserId != userId ? Result<TodoListDto>.Failure(AppErrors.AccessDenied)
            : Result<TodoListDto>.Success(new TodoListDto(list)));
    }

    public Task<Result<PagedResult<TodoItemDto>>> GetTodosAsync(int listId, int userId, bool isAdmin,
        PageRequest page, CancellationToken cancellationToken)
    {
        TodoList? list = _todoLists.FirstOrDefault(list => list.Id == listId);
        if (list == null) return Task.FromResult(Result<PagedResult<TodoItemDto>>.Failure(AppErrors.ListNotFound));
        if (!isAdmin && list.OwnerUserId != userId) return Task.FromResult(Result<PagedResult<TodoItemDto>>.Failure(AppErrors.AccessDenied));
        var items = list.Todos.OrderBy(todo => todo.Id).Skip(page.Offset).Take(page.PageSize)
            .Select(todo => new TodoItemDto(todo)).ToList();
        return Task.FromResult(Result<PagedResult<TodoItemDto>>.Success(new PagedResult<TodoItemDto>(items, list.Todos.Count, page)));
    }

    public Task<Result<TodoItemDto>> GetTodoAsync(int listId, int todoId, int userId, bool isAdmin, CancellationToken cancellationToken)
    {
        TodoList? list = _todoLists.FirstOrDefault(list => list.Id == listId);
        if (list == null) return Task.FromResult(Result<TodoItemDto>.Failure(AppErrors.ListNotFound));
        if (!isAdmin && list.OwnerUserId != userId) return Task.FromResult(Result<TodoItemDto>.Failure(AppErrors.AccessDenied));
        TodoItem? todo = list.Todos.FirstOrDefault(todo => todo.Id == todoId);
        return Task.FromResult(todo == null ? Result<TodoItemDto>.Failure(AppErrors.TodoNotFound)
            : Result<TodoItemDto>.Success(new TodoItemDto(todo)));
    }

    private TodoList? AccessibleList(int listId, int userId, bool isAdmin) =>
        _todoLists.FirstOrDefault(list => list.Id == listId && (isAdmin || list.OwnerUserId == userId));

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

    public void ApplyChanges()
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
