using TodoApi.Application.Contracts;

namespace TodoApi.Application.Common.Execution;

public readonly record struct EmptyRequest;
public readonly record struct Unit;
public sealed record ListIdRequest(int TodoListId);
public sealed record TodoIdRequest(int TodoListId, int TodoId);
public sealed record GetTodosRequest(int TodoListId, PageRequest Page);
public sealed record SetTodoCompletionRequest(int TodoListId, int TodoId, bool IsCompleted);
