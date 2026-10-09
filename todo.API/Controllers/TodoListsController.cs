using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Models.Requests;
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
public sealed class TodoListsController : ControllerBase
{
    private readonly CreateTodoListUseCase _createTodoListUseCase;
    private readonly GetTodoListByIdUseCase _getTodoListByIdUseCase;
    private readonly GetTodoListsUseCase _getTodoListsUseCase;
    private readonly RenameTodoListUseCase _renameTodoListUseCase;
    private readonly DeleteTodoListUseCase _deleteTodoListUseCase;

    public TodoListsController(
        CreateTodoListUseCase createTodoListUseCase,
        GetTodoListByIdUseCase getTodoListByIdUseCase,
        GetTodoListsUseCase getTodoListsUseCase,
        RenameTodoListUseCase renameTodoListUseCase,
        DeleteTodoListUseCase deleteTodoListUseCase)
    {
        _createTodoListUseCase = createTodoListUseCase;
        _getTodoListByIdUseCase = getTodoListByIdUseCase;
        _getTodoListsUseCase = getTodoListsUseCase;
        _renameTodoListUseCase = renameTodoListUseCase;
        _deleteTodoListUseCase = deleteTodoListUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoListDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TodoListDto> todoLists =
            await _getTodoListsUseCase.ExecuteAsync(cancellationToken);

        return Ok(todoLists);
    }

    [HttpPost]
    public async Task<ActionResult<TodoListDto>> Create(
        [FromBody] CreateTodoListRequest request,
        CancellationToken cancellationToken)
    {
        CreateTodoListCommand command = new CreateTodoListCommand
        {
            Name = request.Name
        };

        TodoListDto createdTodoList = await _createTodoListUseCase.ExecuteAsync(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { todoListId = createdTodoList.Id },
            createdTodoList);
    }

    [HttpGet("{todoListId:int}")]
    public async Task<ActionResult<TodoListDto>> GetById(
        [FromRoute] int todoListId,
        CancellationToken cancellationToken)
    {
        TodoListDto? todoList = await _getTodoListByIdUseCase.ExecuteAsync(
            todoListId,
            cancellationToken);

        if (todoList == null)
        {
            return NotFound();
        }

        return Ok(todoList);
    }

    [HttpPut("{todoListId:int}")]
    public async Task<ActionResult<TodoListDto>> Rename(
        [FromRoute] int todoListId,
        [FromBody] RenameTodoListRequest request,
        CancellationToken cancellationToken)
    {
        RenameTodoListCommand command = new RenameTodoListCommand
        {
            TodoListId = todoListId,
            Name = request.Name
        };

        TodoListDto? renamedTodoList = await _renameTodoListUseCase.ExecuteAsync(
            command,
            cancellationToken);

        if (renamedTodoList == null)
        {
            return NotFound();
        }

        return Ok(renamedTodoList);
    }

    [HttpDelete("{todoListId:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int todoListId,
        CancellationToken cancellationToken)
    {
        bool todoListWasDeleted = await _deleteTodoListUseCase.ExecuteAsync(
            todoListId,
            cancellationToken);

        if (todoListWasDeleted == false)
        {
            return NotFound();
        }

        return NoContent();
    }
}
