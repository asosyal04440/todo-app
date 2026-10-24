using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Application.Features.TodoLists.RenameTodoList;

public class RenameTodoListUseCase : UseCase<RenameTodoListCommand, TodoListDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public RenameTodoListUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<RenameTodoListCommand, TodoListDto> pipeline) : base(pipeline)
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

    protected override async Task<Result<TodoListDto>> HandleAsync(
        RenameTodoListCommand command, CancellationToken cancellationToken)
    {
        TodoList? todoList = await _todoListRepository.GetByIdWithTodosAsync(
            command.TodoListId,
            cancellationToken);

        if (todoList == null)
        {
            return Result<TodoListDto>.Failure(AppErrors.ListNotFound);
        }

        bool canAccessTodoList = TodoListAccessPolicy.CanAccess(todoList, _currentUser);

        if (canAccessTodoList == false)
        {
            return Result<TodoListDto>.Failure(AppErrors.AccessDenied);
        }

        bool nameIsTaken = await _todoListRepository.ExistsWithNameForOwnerAsync(
            todoList.OwnerUserId,
            command.Name.Trim(),
            excludedTodoListId: todoList.Id,
            cancellationToken);

        if (nameIsTaken)
        {
            return Result<TodoListDto>.Failure(AppErrors.DuplicateName("You already have a list with the same name."));
        }

        todoList.Rename(command.Name);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TodoListDto response = new TodoListDto(todoList);

        return Result<TodoListDto>.Success(response);
    }
}
