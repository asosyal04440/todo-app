using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Contracts;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Authentication.RegisterWithPassword;
using TodoApi.Application.Features.Authentication.LoginWithPassword;
using TodoApi.Application.Features.Authentication.LoginWithGoogle;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;
using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.RenameTodoList;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.UpdateTodo;

namespace todo.Infrastructure.Tests;

internal static class TestUseCasePipelines
{
    public static UseCasePipeline<CreateTodoListCommand, TodoListDto> ForCreateTodoList(IValidator<CreateTodoListCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<RenameTodoListCommand, TodoListDto> ForRenameTodoList(IValidator<RenameTodoListCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<CreateTodoCommand, TodoItemDto> ForCreateTodo(IValidator<CreateTodoCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<UpdateTodoCommand, TodoItemDto> ForUpdateTodo(IValidator<UpdateTodoCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<RegisterWithPasswordCommand, AuthSession> ForRegisterWithPassword(IValidator<RegisterWithPasswordCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<LoginWithPasswordCommand, AuthSession> ForLoginWithPassword(IValidator<LoginWithPasswordCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<LoginWithGoogleCommand, AuthSession> ForLoginWithGoogle(IValidator<LoginWithGoogleCommand> validator) =>
        new([validator], NullLoggerFactory.Instance);

    public static UseCasePipeline<ListIdRequest, Unit> ForDeleteTodoList() =>
        new([new ListIdRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<TodoIdRequest, Unit> ForDeleteTodo() =>
        new([new TodoIdRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<SetTodoCompletionRequest, TodoItemDto> ForSetTodoCompletionStatus() =>
        new([new SetTodoCompletionRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<string?, AuthSession> ForRefreshSession() =>
        new([], NullLoggerFactory.Instance);

    public static UseCasePipeline<string?, Unit> ForLogout() =>
        new([], NullLoggerFactory.Instance);

    public static UseCasePipeline<EmptyRequest, UserDto> ForGetCurrentUser() =>
        new([], NullLoggerFactory.Instance);

    public static UseCasePipeline<EmptyRequest, ReminderRunResult> ForSendDueTodoReminders() =>
        new([], NullLoggerFactory.Instance);

    public static UseCasePipeline<PageRequest, PagedResult<TodoListDto>> ForGetTodoLists() =>
        new([new PageRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<ListIdRequest, TodoListDto> ForGetTodoListById() =>
        new([new ListIdRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<GetTodosRequest, PagedResult<TodoItemDto>> ForGetTodosInList() =>
        new([new GetTodosRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<TodoIdRequest, TodoItemDto> ForGetTodoById() =>
        new([new TodoIdRequestValidator()], NullLoggerFactory.Instance);

    public static UseCasePipeline<PageRequest, PagedResult<UserDto>> ForGetUsers() =>
        new([new PageRequestValidator()], NullLoggerFactory.Instance);
}