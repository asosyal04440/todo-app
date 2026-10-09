using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Validation;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Application.Features.TodoLists.CreateTodoList;

public class CreateTodoListUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateTodoListCommand> _validator;

    public CreateTodoListUseCase(
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        IValidator<CreateTodoListCommand> validator)
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

        _todoListRepository = todoListRepository;
        _currentUser = currentUser;
        _validator = validator;
    }

    public async Task<TodoListDto> ExecuteAsync(
        CreateTodoListCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateOrThrowAsync(command, cancellationToken);

        bool nameIsTaken = await _todoListRepository.ExistsWithNameForOwnerAsync(
            _currentUser.UserId,
            command.Name.Trim(),
            excludedTodoListId: null,
            cancellationToken);

        if (nameIsTaken)
        {
            throw new DuplicateNameException("You already have a list with the same name.");
        }

        TodoList newTodoList = new TodoList(command.Name, _currentUser.UserId);

        await _todoListRepository.AddAsync(newTodoList, cancellationToken);
        await _todoListRepository.SaveChangesAsync(cancellationToken);

        TodoListDto response = new TodoListDto(newTodoList);

        return response;
    }
}
