using FluentValidation;
using FluentValidation.Results;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Common.Validation;
using TodoApi.Application.Contracts;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.UpdateTodo;

public class UpdateTodoUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<UpdateTodoCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public UpdateTodoUseCase(
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        IValidator<UpdateTodoCommand> validator,
        TimeProvider timeProvider)
    {
        if (todoListRepository == null)
        {
            throw new ArgumentNullException(nameof(todoListRepository));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        if (validator == null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _todoListRepository = todoListRepository;
        _currentUser = currentUser;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<TodoItemDto?> ExecuteAsync(
        UpdateTodoCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateOrThrowAsync(command, cancellationToken);

        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            command.TodoListId,
            cancellationToken);

        if (todoList == null)
        {
            return null;
        }

        bool canAccessTodoList = TodoListAccessPolicy.CanAccess(todoList, _currentUser);

        if (canAccessTodoList == false)
        {
            return null;
        }

        TodoItem? todoToUpdate = null;

        foreach (TodoItem todoItem in todoList.Todos)
        {
            if (todoItem.Id == command.TodoId)
            {
                todoToUpdate = todoItem;
                break;
            }
        }

        if (todoToUpdate == null)
        {
            return null;
        }

        EnsureAChangedDueDateIsNotInThePast(todoToUpdate, command.DueAtUtc);

        todoList.RenameTodo(todoToUpdate, command.Title);
        todoToUpdate.ChangeDescription(command.Description);
        todoToUpdate.SetDueAtUtc(command.DueAtUtc);
        todoToUpdate.ChangePriority(command.Priority);

        await _todoListRepository.SaveChangesAsync(cancellationToken);

        TodoItemDto response = new TodoItemDto(todoToUpdate);

        return response;
    }

    private void EnsureAChangedDueDateIsNotInThePast(TodoItem todo, DateTime? requestedDueAtUtc)
    {
        if (requestedDueAtUtc.HasValue == false || requestedDueAtUtc == todo.DueAtUtc)
        {
            return;
        }

        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (SharedValidationRules.IsTodoDueDateInThePast(requestedDueAtUtc.Value, nowUtc))
        {
            throw new RequestValidationException(new[]
            {
                new ValidationFailure(
                    nameof(UpdateTodoCommand.DueAtUtc),
                    SharedValidationRules.TodoDueDateInThePastMessage)
            });
        }
    }
}
