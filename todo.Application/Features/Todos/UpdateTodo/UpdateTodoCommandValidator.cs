using FluentValidation;
using TodoApi.Application.Common.Validation;

namespace TodoApi.Application.Features.Todos.UpdateTodo;

public sealed class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator(TimeProvider timeProvider)
    {
        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        RuleFor(command => command.TodoListId).EntityId();
        RuleFor(command => command.TodoId).EntityId();
        RuleFor(command => command.Title).TodoTitle();
        RuleFor(command => command.Description).TodoDescription();
        RuleFor(command => command.DueAtUtc).TodoDueDate(timeProvider);
        RuleFor(command => command.Priority).TodoPriority();
    }
}
