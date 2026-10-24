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

namespace TodoApi.Application.Features.Todos.CreateTodo;

public class CreateTodoUseCase : UseCase<CreateTodoCommand, TodoItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public CreateTodoUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<CreateTodoCommand, TodoItemDto> pipeline) : base(pipeline)
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

    protected override async Task<Result<TodoItemDto>> HandleAsync(
        CreateTodoCommand command, CancellationToken cancellationToken)
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

        TodoItem newTodo = todoList.AddTodo(
            command.Title,
            command.Description,
            command.DueAtUtc,
            command.Priority);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TodoItemDto response = new TodoItemDto(newTodo);

        return Result<TodoItemDto>.Success(response);
    }
}
