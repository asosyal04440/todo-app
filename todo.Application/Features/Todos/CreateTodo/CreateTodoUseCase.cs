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

namespace TodoApi.Application.Features.Todos.CreateTodo;

public class CreateTodoUseCase
{
    private readonly ITodoListRepository _todoListRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateTodoCommand> _validator;

    public CreateTodoUseCase(
        ITodoListRepository todoListRepository,
        ICurrentUser currentUser,
        IValidator<CreateTodoCommand> validator)
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

    public async Task<TodoItemDto?> ExecuteAsync(
        CreateTodoCommand command,
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

        TodoItem newTodo = todoList.AddTodo(
            command.Title,
            command.Description,
            command.DueAtUtc,
            command.Priority);

        await _todoListRepository.SaveChangesAsync(cancellationToken);

        TodoItemDto response = new TodoItemDto(newTodo);

        return response;
    }
}
