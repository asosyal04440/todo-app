using FluentValidation;
using TodoApi.Application.Common.Validation;

namespace TodoApi.Application.Features.TodoLists.RenameTodoList;

public sealed class RenameTodoListCommandValidator : AbstractValidator<RenameTodoListCommand>
{
    public RenameTodoListCommandValidator()
    {
        RuleFor(command => command.TodoListId).EntityId();
        RuleFor(command => command.Name).TodoListName();
    }
}
