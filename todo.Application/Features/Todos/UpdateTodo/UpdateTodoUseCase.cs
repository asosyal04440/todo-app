using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Common.Validation;
using FluentValidation.Results;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Contracts;
using TodoApi.Application.Exceptions;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.UpdateTodo;

public class UpdateTodoUseCase : UseCase<UpdateTodoCommand, TodoItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateTodoUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<UpdateTodoCommand, TodoItemDto> pipeline,
        TimeProvider timeProvider) : base(pipeline)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

        if (todoListRepository == null)
        {
            throw new ArgumentNullException(nameof(todoListRepository));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _unitOfWork = unitOfWork;
        _todoListRepository = todoListRepository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    protected override async Task<Result<TodoItemDto>> HandleAsync(
        UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            command.TodoListId,
            cancellationToken);

        if (todoList == null)
        {
            return Result<TodoItemDto>.Failure(AppErrors.ListNotFound);
        }

        bool canAccessTodoList = TodoListAccessPolicy.CanAccess(todoList, _currentUser);

        if (canAccessTodoList == false)
        {
            return Result<TodoItemDto>.Failure(AppErrors.AccessDenied);
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
            return Result<TodoItemDto>.Failure(AppErrors.TodoNotFound);
        }

        EnsureAChangedDueDateIsNotInThePast(todoToUpdate, command.DueAtUtc);

        todoList.RenameTodo(todoToUpdate, command.Title);
        todoToUpdate.ChangeDescription(command.Description);
        todoToUpdate.SetDueAtUtc(command.DueAtUtc);
        todoToUpdate.ChangePriority(command.Priority);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TodoItemDto response = new TodoItemDto(todoToUpdate);

        return Result<TodoItemDto>.Success(response);
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
