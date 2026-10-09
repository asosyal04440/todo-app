using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Authorization;
using TodoApi.Application.Common.Validation;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Exceptions;

namespace TodoApi.Application.Features.TodoLists.RenameTodoList;

public class RenameTodoListUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<RenameTodoListCommand> _validator;

    public RenameTodoListUseCase(
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        IValidator<RenameTodoListCommand> validator)
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

    public async Task<TodoListDto?> ExecuteAsync(
        RenameTodoListCommand command,
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

        bool nameIsTaken = await _todoListRepository.ExistsWithNameForOwnerAsync(
            todoList.OwnerUserId,
            command.Name.Trim(),
            excludedTodoListId: todoList.Id,
            cancellationToken);

        if (nameIsTaken)
        {
            throw new DuplicateNameException("You already have a list with the same name.");
        }

        todoList.Rename(command.Name);

        await _todoListRepository.SaveChangesAsync(cancellationToken);

        TodoListDto response = new TodoListDto(todoList);

        return response;
    }
}
