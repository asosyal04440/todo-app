using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace todo.Domain.Tests;

public class TodoItemTests
{
    private const int OwnerUserId = 1;

    [Fact]
    public void AddTodo_RejectsAnEmptyTitle()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);

        Assert.Throws<DomainRuleViolationException>(
            () => todoList.AddTodo("   "));
    }

    [Fact]
    public void AddTodo_RejectsATitleLongerThanOneHundredTwentyCharacters()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);
        string longTitle = new string('a', 121);

        Assert.Throws<DomainRuleViolationException>(
            () => todoList.AddTodo(longTitle));
    }

    [Fact]
    public void AddTodo_TrimsTheTitleAndDescription()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);

        TodoItem todoItem = todoList.AddTodo(
            "  Faturayı öde  ",
            "  Son ödeme tarihini kontrol et  ");

        Assert.Equal("Faturayı öde", todoItem.Title);
        Assert.Equal("Son ödeme tarihini kontrol et", todoItem.Description);
    }

    [Fact]
    public void AddTodo_TreatsAnEmptyDescriptionAsNull()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);

        TodoItem todoItem = todoList.AddTodo("Faturayı öde", "   ");

        Assert.Null(todoItem.Description);
    }

    [Fact]
    public void AddTodo_RejectsADescriptionLongerThanOneThousandCharacters()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);
        string longDescription = new string('a', 1001);

        Assert.Throws<DomainRuleViolationException>(
            () => todoList.AddTodo("Faturayı öde", longDescription));
    }

    [Fact]
    public void AddTodo_RejectsADueDateThatIsNotUtc()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);
        DateTime localDueDate = DateTime.SpecifyKind(
            new DateTime(2026, 9, 26, 18, 0, 0),
            DateTimeKind.Local);

        Assert.Throws<DomainRuleViolationException>(
            () => todoList.AddTodo("Faturayı öde", dueAtUtc: localDueDate));
    }

    [Fact]
    public void AddTodo_AcceptsAUtcDueDate()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);
        DateTime dueAtUtc = new DateTime(
            2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);

        TodoItem todoItem = todoList.AddTodo("Faturayı öde", dueAtUtc: dueAtUtc);

        Assert.Equal(dueAtUtc, todoItem.DueAtUtc);
    }

    [Fact]
    public void AddTodo_RejectsAnUnsupportedPriority()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);
        TodoPriority unsupportedPriority = (TodoPriority)999;

        Assert.Throws<DomainRuleViolationException>(
            () => todoList.AddTodo("Faturayı öde", priority: unsupportedPriority));
    }

    [Fact]
    public void Rename_ChangesTheTitle()
    {
        TodoList todoList = new TodoList("İş", OwnerUserId);
        TodoItem todoItem = todoList.AddTodo("Eski başlık");

        todoList.RenameTodo(todoItem, "  Yeni başlık  ");

        Assert.Equal("Yeni başlık", todoItem.Title);
    }

    [Fact]
    public void MarkCompletedAndMarkIncomplete_ChangeTheCompletionState()
    {
        TodoList todoList = new TodoList("İş", OwnerUserId);
        TodoItem todoItem = todoList.AddTodo("Raporu tamamla");

        todoItem.MarkCompleted();

        Assert.True(todoItem.IsCompleted);

        todoItem.MarkIncomplete();

        Assert.False(todoItem.IsCompleted);
    }
}
