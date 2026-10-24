using FluentValidation;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Common.Execution;

public sealed class ListIdRequestValidator : AbstractValidator<ListIdRequest>
{
    public ListIdRequestValidator() => RuleFor(request => request.TodoListId).GreaterThan(0);
}

public sealed class TodoIdRequestValidator : AbstractValidator<TodoIdRequest>
{
    public TodoIdRequestValidator()
    {
        RuleFor(request => request.TodoListId).GreaterThan(0);
        RuleFor(request => request.TodoId).GreaterThan(0);
    }
}

public sealed class GetTodosRequestValidator : AbstractValidator<GetTodosRequest>
{
    public GetTodosRequestValidator()
    {
        RuleFor(request => request.TodoListId).GreaterThan(0);
        RuleFor(request => request.Page).NotNull().SetValidator(new PageRequestValidator());
    }
}

public sealed class SetTodoCompletionRequestValidator : AbstractValidator<SetTodoCompletionRequest>
{
    public SetTodoCompletionRequestValidator()
    {
        RuleFor(request => request.TodoListId).GreaterThan(0);
        RuleFor(request => request.TodoId).GreaterThan(0);
    }
}

public sealed class PageRequestValidator : AbstractValidator<PageRequest>
{
    public PageRequestValidator()
    {
        RuleFor(request => request.PageNumber).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, PageRequest.MaximumPageSize);
    }
}
