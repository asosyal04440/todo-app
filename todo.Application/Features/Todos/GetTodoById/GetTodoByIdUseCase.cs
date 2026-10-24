using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Features.Todos.GetTodoById;

public class GetTodoByIdUseCase(ITodoReadQueries queries, ICurrentUser currentUser, UseCasePipeline<TodoIdRequest, TodoItemDto> pipeline) : UseCase<TodoIdRequest, TodoItemDto>(pipeline)
{
    public Task<Result<TodoItemDto>> ExecuteAsync(int todoListId, int todoId, CancellationToken cancellationToken = default) => base.ExecuteAsync(new TodoIdRequest(todoListId, todoId), cancellationToken);
    protected override Task<Result<TodoItemDto>> HandleAsync(TodoIdRequest request, CancellationToken cancellationToken) =>
        queries.GetTodoAsync(request.TodoListId, request.TodoId, currentUser.UserId, currentUser.IsAdmin, cancellationToken);
}