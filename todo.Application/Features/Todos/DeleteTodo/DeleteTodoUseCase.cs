using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Todos.DeleteTodo;

public class DeleteTodoUseCase : UseCase<TodoIdRequest, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public DeleteTodoUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<TodoIdRequest, Unit> pipeline) : base(pipeline)
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

    public Task<Result<Unit>> ExecuteAsync(int todoListId, int todoId, CancellationToken cancellationToken = default) =>
        base.ExecuteAsync(new TodoIdRequest(todoListId, todoId), cancellationToken);

    protected override async Task<Result<Unit>> HandleAsync(
        TodoIdRequest request, CancellationToken cancellationToken)
    {
        int todoListId = request.TodoListId;
        int todoId = request.TodoId;
        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            todoListId,
            cancellationToken);

        if (todoList == null)
        {
            return Result<Unit>.Failure(AppErrors.ListNotFound);
        }

        bool canAccessTodoList = TodoListAccessPolicy.CanAccess(todoList, _currentUser);

        if (canAccessTodoList == false)
        {
            return Result<Unit>.Failure(AppErrors.AccessDenied);
        }

        bool todoWasRemoved = todoList.RemoveTodo(todoId);

        if (todoWasRemoved == false)
        {
            return Result<Unit>.Failure(AppErrors.TodoNotFound);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(default);
    }
}
