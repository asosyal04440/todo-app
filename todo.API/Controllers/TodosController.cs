using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Models.Requests;
using TodoApi.Application.Contracts;
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
public sealed class TodosController : ControllerBase
{
    private readonly CreateTodoUseCase _createTodoUseCase;
    private readonly GetTodosInListUseCase _getTodosInListUseCase;
    private readonly GetTodoByIdUseCase _getTodoByIdUseCase;
    private readonly UpdateTodoUseCase _updateTodoUseCase;
    private readonly SetTodoCompletionStatusUseCase _setTodoCompletionStatusUseCase;
    private readonly DeleteTodoUseCase _deleteTodoUseCase;

    public TodosController(
        CreateTodoUseCase createTodoUseCase,
        GetTodosInListUseCase getTodosInListUseCase,
        GetTodoByIdUseCase getTodoByIdUseCase,
        UpdateTodoUseCase updateTodoUseCase,
        SetTodoCompletionStatusUseCase setTodoCompletionStatusUseCase,
        DeleteTodoUseCase deleteTodoUseCase)
    {
        _createTodoUseCase = createTodoUseCase;
        _getTodosInListUseCase = getTodosInListUseCase;
        _getTodoByIdUseCase = getTodoByIdUseCase;
        _updateTodoUseCase = updateTodoUseCase;
        _setTodoCompletionStatusUseCase = setTodoCompletionStatusUseCase;
        _deleteTodoUseCase = deleteTodoUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoItemDto>>> GetForList(
        [FromRoute] int todoListId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TodoItemDto>? todos = await _getTodosInListUseCase.ExecuteAsync(
            todoListId,
            cancellationToken);

        if (todos == null)
        {
            return NotFound();
        }

        return Ok(todos);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemDto>> Create(
        [FromRoute] int todoListId,
        [FromBody] CreateTodoRequest request,
        CancellationToken cancellationToken)
    {
        CreateTodoCommand command = new CreateTodoCommand
        {
            TodoListId = todoListId,
            Title = request.Title,
            Description = request.Description,
            DueAtUtc = request.DueAtUtc,
            Priority = request.Priority
        };

        TodoItemDto? createdTodo = await _createTodoUseCase.ExecuteAsync(
            command,
            cancellationToken);

        if (createdTodo == null)
        {
            return NotFound();
        }

        return CreatedAtAction(
            nameof(GetById),
            new { todoListId, todoId = createdTodo.Id },
            createdTodo);
    }

    [HttpGet("{todoId:int}")]
    public async Task<ActionResult<TodoItemDto>> GetById(
        [FromRoute] int todoListId,
        [FromRoute] int todoId,
        CancellationToken cancellationToken)
    {
        TodoItemDto? todo = await _getTodoByIdUseCase.ExecuteAsync(
            todoListId,
            todoId,
            cancellationToken);

        if (todo == null)
        {
            return NotFound();
        }

        return Ok(todo);
    }

    [HttpPut("{todoId:int}")]
    public async Task<ActionResult<TodoItemDto>> Update(
        [FromRoute] int todoListId,
        [FromRoute] int todoId,
        [FromBody] UpdateTodoRequest request,
        CancellationToken cancellationToken)
    {
        UpdateTodoCommand command = new UpdateTodoCommand
        {
            TodoListId = todoListId,
            TodoId = todoId,
            Title = request.Title,
            Description = request.Description,
            DueAtUtc = request.DueAtUtc,
            Priority = request.Priority
        };

        TodoItemDto? updatedTodo = await _updateTodoUseCase.ExecuteAsync(
            command,
            cancellationToken);

        if (updatedTodo == null)
        {
            return NotFound();
        }

        return Ok(updatedTodo);
    }

    [HttpPatch("{todoId:int}/completion")]
    public async Task<ActionResult<TodoItemDto>> SetCompletionStatus(
        [FromRoute] int todoListId,
        [FromRoute] int todoId,
        [FromBody] SetTodoCompletionStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IsCompleted.HasValue == false)
        {
            return BadRequest();
        }

        TodoItemDto? updatedTodo = await _setTodoCompletionStatusUseCase.ExecuteAsync(
            todoListId,
            todoId,
            request.IsCompleted.Value,
            cancellationToken);

        if (updatedTodo == null)
        {
            return NotFound();
        }

        return Ok(updatedTodo);
    }

    [HttpDelete("{todoId:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] int todoListId,
        [FromRoute] int todoId,
        CancellationToken cancellationToken)
    {
        bool todoWasDeleted = await _deleteTodoUseCase.ExecuteAsync(
            todoListId,
            todoId,
            cancellationToken);

        if (todoWasDeleted == false)
        {
            return NotFound();
        }

        return NoContent();
    }
}
