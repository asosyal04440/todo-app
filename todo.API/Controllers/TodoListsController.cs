using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Models.Requests;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Contracts;
using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.DeleteTodoList;
using TodoApi.Application.Features.TodoLists.GetTodoListById;
using TodoApi.Application.Features.TodoLists.GetTodoLists;
using TodoApi.Application.Features.TodoLists.RenameTodoList;

namespace TodoApi.API.Controllers;

[ApiController]
[Authorize]
[Route("api/todo-lists")]
public sealed class TodoListsController(CreateTodoListUseCase create, GetTodoListByIdUseCase getById,
    GetTodoListsUseCase getAll, RenameTodoListUseCase rename, DeleteTodoListUseCase delete) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<TodoListDto>>> GetAll([FromQuery] PageQuery query, CancellationToken cancellationToken) =>
        (await getAll.ExecuteAsync(query.ToPageRequest(), cancellationToken)).ToActionResult(this);

    [HttpPost]
    public async Task<ActionResult<TodoListDto>> Create([FromBody] CreateTodoListRequest request, CancellationToken cancellationToken) =>
        (await create.ExecuteAsync(new CreateTodoListCommand { Name = request.Name }, cancellationToken))
            .ToActionResult(this, value => CreatedAtAction(nameof(GetById), new { todoListId = value.Id }, value));

    [HttpGet("{todoListId:int}")]
    public async Task<ActionResult<TodoListDto>> GetById([FromRoute] int todoListId, CancellationToken cancellationToken) =>
        (await getById.ExecuteAsync(todoListId, cancellationToken)).ToActionResult(this);

    [HttpPut("{todoListId:int}")]
    public async Task<ActionResult<TodoListDto>> Rename([FromRoute] int todoListId, [FromBody] RenameTodoListRequest request, CancellationToken cancellationToken) =>
        (await rename.ExecuteAsync(new RenameTodoListCommand { TodoListId = todoListId, Name = request.Name }, cancellationToken))
            .ToActionResult(this);

    [HttpDelete("{todoListId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int todoListId, CancellationToken cancellationToken) =>
        (await delete.ExecuteAsync(todoListId, cancellationToken)).ToActionResult(this, _ => NoContent());
}