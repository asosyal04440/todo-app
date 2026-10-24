using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.TodoLists.DeleteTodoList;

public class DeleteTodoListUseCase : UseCase<ListIdRequest, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public DeleteTodoListUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<ListIdRequest, Unit> pipeline) : base(pipeline)
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

    public Task<Result<Unit>> ExecuteAsync(int todoListId, CancellationToken cancellationToken = default) =>
        base.ExecuteAsync(new ListIdRequest(todoListId), cancellationToken);

    protected override async Task<Result<Unit>> HandleAsync(
        ListIdRequest request, CancellationToken cancellationToken)
    {
        int todoListId = request.TodoListId;
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

        _todoListRepository.Delete(todoList);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(default);
    }
}
