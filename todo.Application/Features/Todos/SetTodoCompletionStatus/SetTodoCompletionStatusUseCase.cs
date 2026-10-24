using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.SetTodoCompletionStatus;

public class SetTodoCompletionStatusUseCase : UseCase<SetTodoCompletionRequest, TodoItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public SetTodoCompletionStatusUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<SetTodoCompletionRequest, TodoItemDto> pipeline) : base(pipeline)
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

        _unitOfWork = unitOfWork;
        _todoListRepository = todoListRepository;
        _currentUser = currentUser;
    }

    public Task<Result<TodoItemDto>> ExecuteAsync(int todoListId, int todoId, bool isCompleted, CancellationToken cancellationToken = default) =>
        base.ExecuteAsync(new SetTodoCompletionRequest(todoListId, todoId, isCompleted), cancellationToken);

    protected override async Task<Result<TodoItemDto>> HandleAsync(
        SetTodoCompletionRequest request, CancellationToken cancellationToken)
    {
        int todoListId = request.TodoListId;
        int todoId = request.TodoId;
        bool isCompleted = request.IsCompleted;
        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            todoListId,
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
            if (todoItem.Id == todoId)
            {
                todoToUpdate = todoItem;
                break;
            }
        }

        if (todoToUpdate == null)
        {
            return Result<TodoItemDto>.Failure(AppErrors.TodoNotFound);
        }

        if (isCompleted)
        {
            todoToUpdate.MarkCompleted();
        }
        else
        {
            todoToUpdate.MarkIncomplete();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TodoItemDto response = new TodoItemDto(todoToUpdate);

        return Result<TodoItemDto>.Success(response);
    }
}
