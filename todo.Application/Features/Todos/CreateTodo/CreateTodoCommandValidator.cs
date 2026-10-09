using FluentValidation;
using TodoApi.Application.Common.Validation;

namespace TodoApi.Application.Features.Todos.CreateTodo;

public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator(TimeProvider timeProvider)
    {
        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        RuleFor(command => command.TodoListId).EntityId();
        RuleFor(command => command.Title).TodoTitle();
        RuleFor(command => command.Description).TodoDescription();
        RuleFor(command => command.DueAtUtc).NewTodoDueDate(timeProvider);
        RuleFor(command => command.Priority).TodoPriority();
    }
}
