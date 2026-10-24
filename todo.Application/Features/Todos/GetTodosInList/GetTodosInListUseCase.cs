using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Features.Todos.GetTodosInList;

public class GetTodosInListUseCase(ITodoReadQueries queries, ICurrentUser currentUser, UseCasePipeline<GetTodosRequest, PagedResult<TodoItemDto>> pipeline) : UseCase<GetTodosRequest, PagedResult<TodoItemDto>>(pipeline)
{
    public Task<Result<PagedResult<TodoItemDto>>> ExecuteAsync(int todoListId, CancellationToken cancellationToken = default) => ExecuteAsync(todoListId, new PageRequest(), cancellationToken);
    public Task<Result<PagedResult<TodoItemDto>>> ExecuteAsync(int todoListId, PageRequest page, CancellationToken cancellationToken = default) => base.ExecuteAsync(new GetTodosRequest(todoListId, page), cancellationToken);
    protected override Task<Result<PagedResult<TodoItemDto>>> HandleAsync(GetTodosRequest request, CancellationToken cancellationToken) =>
        queries.GetTodosAsync(request.TodoListId, currentUser.UserId, currentUser.IsAdmin, request.Page, cancellationToken);
}