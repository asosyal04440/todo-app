using FluentValidation;
using TodoApi.Application.Common.Validation;

namespace TodoApi.Application.Features.TodoLists.CreateTodoList;

public sealed class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    public CreateTodoListCommandValidator()
    {
        RuleFor(command => command.Name).TodoListName();
    }
}
