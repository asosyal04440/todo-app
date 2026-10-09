using System;
using System.Globalization;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Application.Features.Authentication.Common;
using TodoApi.Application.Features.Authentication.GetCurrentUser;
using TodoApi.Application.Features.Authentication.LoginWithGoogle;
using TodoApi.Application.Features.Authentication.LoginWithPassword;
using TodoApi.Application.Features.Authentication.Logout;
using TodoApi.Application.Features.Authentication.RefreshSession;
using TodoApi.Application.Features.Authentication.RegisterWithPassword;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;
using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.DeleteTodoList;
using TodoApi.Application.Features.TodoLists.GetTodoListById;
using TodoApi.Application.Features.TodoLists.GetTodoLists;
using TodoApi.Application.Features.TodoLists.RenameTodoList;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.DeleteTodo;
using TodoApi.Application.Features.Todos.GetTodoById;
using TodoApi.Application.Features.Todos.GetTodosInList;
using TodoApi.Application.Features.Todos.SetTodoCompletionStatus;
using TodoApi.Application.Features.Todos.UpdateTodo;
using TodoApi.Application.Features.Users.GetUsers;

namespace TodoApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("tr-TR");

        services.AddValidatorsFromAssemblyContaining<CreateTodoListCommandValidator>();

        services.AddScoped<CreateTodoListUseCase>();
        services.AddScoped<GetTodoListByIdUseCase>();
        services.AddScoped<GetTodoListsUseCase>();
        services.AddScoped<RenameTodoListUseCase>();
        services.AddScoped<DeleteTodoListUseCase>();
        services.AddScoped<CreateTodoUseCase>();
        services.AddScoped<GetTodosInListUseCase>();
        services.AddScoped<GetTodoByIdUseCase>();
        services.AddScoped<UpdateTodoUseCase>();
        services.AddScoped<SetTodoCompletionStatusUseCase>();
        services.AddScoped<DeleteTodoUseCase>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<RegisterWithPasswordUseCase>();
        services.AddScoped<LoginWithPasswordUseCase>();
        services.AddScoped<LoginWithGoogleUseCase>();
        services.AddScoped<RefreshSessionUseCase>();
        services.AddScoped<LogoutUseCase>();
        services.AddScoped<GetCurrentUserUseCase>();
        services.AddScoped<GetUsersUseCase>();
        services.AddScoped<SendDueTodoRemindersUseCase>();

        return services;
    }
}
