using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Features.TodoLists.GetTodoLists;

public class GetTodoListsUseCase(ITodoReadQueries queries, ICurrentUser currentUser, UseCasePipeline<PageRequest, PagedResult<TodoListDto>> pipeline) : UseCase<PageRequest, PagedResult<TodoListDto>>(pipeline)
{
    public Task<Result<PagedResult<TodoListDto>>> ExecuteAsync(CancellationToken cancellationToken = default) => base.ExecuteAsync(new PageRequest(), cancellationToken);
    protected override async Task<Result<PagedResult<TodoListDto>>> HandleAsync(PageRequest page, CancellationToken cancellationToken) =>
        Result<PagedResult<TodoListDto>>.Success(await queries.GetListsAsync(currentUser.UserId, page, cancellationToken));
}