using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Models.Requests;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Contracts;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.DeleteTodo;
using TodoApi.Application.Features.Todos.GetTodoById;
using TodoApi.Application.Features.Todos.GetTodosInList;
using TodoApi.Application.Features.Todos.SetTodoCompletionStatus;
using TodoApi.Application.Features.Todos.UpdateTodo;

namespace TodoApi.API.Controllers;

[ApiController]
[Authorize]
[Route("api/todo-lists/{todoListId:int}/todos")]
public sealed class TodosController(CreateTodoUseCase create, GetTodosInListUseCase getForList, GetTodoByIdUseCase getById,
    UpdateTodoUseCase update, SetTodoCompletionStatusUseCase setCompletion, DeleteTodoUseCase delete) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TodoItemDto>>> GetForList([FromRoute] int todoListId, [FromQuery] PageQuery query, CancellationToken cancellationToken) =>
        (await getForList.ExecuteAsync(todoListId, query.ToPageRequest(), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<TodoItemDto>> Create([FromRoute] int todoListId, [FromBody] CreateTodoRequest request, CancellationToken cancellationToken) =>
        (await create.ExecuteAsync(new CreateTodoCommand { TodoListId = todoListId, Title = request.Title,
            Description = request.Description, DueAtUtc = request.DueAtUtc, Priority = request.Priority }, cancellationToken))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { todoListId, todoId = value.Id }, value));

    [HttpGet("{todoId:int}")]
    public async Task<ActionResult<TodoItemDto>> GetById([FromRoute] int todoListId, [FromRoute] int todoId, CancellationToken cancellationToken) =>
        (await getById.ExecuteAsync(todoListId, todoId, cancellationToken)).ToActionResult(this);

    [HttpPut("{todoId:int}")]
    public async Task<ActionResult<TodoItemDto>> Update([FromRoute] int todoListId, [FromRoute] int todoId,
        [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken) =>
        (await update.ExecuteAsync(new UpdateTodoCommand { TodoListId = todoListId, TodoId = todoId, Title = request.Title,
            Description = request.Description, DueAtUtc = request.DueAtUtc, Priority = request.Priority }, cancellationToken))
            .ToActionResult(this);

    [HttpPatch("{todoId:int}/completion")]
    public async Task<ActionResult<TodoItemDto>> SetCompletionStatus([FromRoute] int todoListId, [FromRoute] int todoId,
        [FromBody] SetTodoCompletionStatusRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsCompleted.HasValue)
            return Result<TodoItemDto>.Failure(
                AppErrors.Validation(new Dictionary<string, string[]> { ["IsCompleted"] = ["A completion status is required."] }))
                .ToActionResult(this);
        return (await setCompletion.ExecuteAsync(todoListId, todoId, request.IsCompleted.Value, cancellationToken)).ToActionResult(this);
    }

    [HttpDelete("{todoId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int todoListId, [FromRoute] int todoId, CancellationToken cancellationToken) =>
        (await delete.ExecuteAsync(todoListId, todoId, cancellationToken)).ToActionResult(this, _ => NoContent());
}
