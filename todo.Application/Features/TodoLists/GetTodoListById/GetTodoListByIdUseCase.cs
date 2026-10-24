using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Features.TodoLists.GetTodoListById;

public class GetTodoListByIdUseCase(ITodoReadQueries queries, ICurrentUser currentUser, UseCasePipeline<ListIdRequest, TodoListDto> pipeline) : UseCase<ListIdRequest, TodoListDto>(pipeline)
{
    public Task<Result<TodoListDto>> ExecuteAsync(int todoListId, CancellationToken cancellationToken = default) => base.ExecuteAsync(new ListIdRequest(todoListId), cancellationToken);
    protected override Task<Result<TodoListDto>> HandleAsync(ListIdRequest request, CancellationToken cancellationToken) =>
        queries.GetListAsync(request.TodoListId, currentUser.UserId, currentUser.IsAdmin, cancellationToken);
}