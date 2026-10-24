using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Application.Features.TodoLists.CreateTodoList;

public class CreateTodoListUseCase : UseCase<CreateTodoListCommand, TodoListDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;

    public CreateTodoListUseCase(
        IUnitOfWork unitOfWork,
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        UseCasePipeline<CreateTodoListCommand, TodoListDto> pipeline) : base(pipeline)
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
        CreateTodoListCommand command, CancellationToken cancellationToken)
    {
        bool nameIsTaken = await _todoListRepository.ExistsWithNameForOwnerAsync(
            _currentUser.UserId,
            command.Name.Trim(),
            excludedTodoListId: null,
            cancellationToken);

        if (nameIsTaken)
        {
            return Result<TodoListDto>.Failure(AppErrors.DuplicateName("You already have a list with the same name."));
        }

        TodoList newTodoList = new TodoList(command.Name, _currentUser.UserId);

        await _todoListRepository.AddAsync(newTodoList, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        TodoListDto response = new TodoListDto(newTodoList);

        return Result<TodoListDto>.Success(response);
    }
}
