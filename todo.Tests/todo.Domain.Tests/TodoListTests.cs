using TodoApi.Domain.Entities;
using TodoApi.Domain.Enums;
using TodoApi.Domain.Exceptions;

namespace todo.Domain.Tests;

public class TodoListTests
{
    private const int OwnerUserId = 1;

    [Fact]
    public void Constructor_TrimsTheListName()
    {
        TodoList todoList = new TodoList("  Ev işleri  ", OwnerUserId);

        Assert.Equal("Ev işleri", todoList.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsAnEmptyListName(string name)
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new TodoList(name, OwnerUserId));
    }

    [Fact]
    public void Constructor_RejectsAListNameLongerThanEightyCharacters()
    {
        string longName = new string('a', 81);

        Assert.Throws<DomainRuleViolationException>(
            () => new TodoList(longName, OwnerUserId));
    }

    [Fact]
    public void Rename_ReplacesTheNameAndTrimsIt()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);

        todoList.Rename("  Alışveriş  ");

        Assert.Equal("Alışveriş", todoList.Name);
    }

    [Fact]
    public void AddTodo_CreatesATodoWithTheProvidedValues()
    {
        TodoList todoList = new TodoList("İş", OwnerUserId);

        TodoItem todoItem = todoList.AddTodo(
            "  Raporu tamamla  ",
            "  Sonuçları ekle  ",
            priority: TodoPriority.High);

        Assert.Equal("Raporu tamamla", todoItem.Title);
        Assert.Equal("Sonuçları ekle", todoItem.Description);
        Assert.Equal(TodoPriority.High, todoItem.Priority);
        Assert.False(todoItem.IsCompleted);
        Assert.Contains(todoItem, todoList.Todos);
    }

    [Fact]
    public void Constructor_RecordsTheOwner()
    {
        TodoList todoList = new TodoList("Ev", OwnerUserId);

        Assert.Equal(OwnerUserId, todoList.OwnerUserId);
        Assert.True(todoList.IsOwnedBy(OwnerUserId));
        Assert.False(todoList.IsOwnedBy(OwnerUserId + 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsAListWithoutAValidOwner(int ownerUserId)
    {
        Assert.Throws<DomainRuleViolationException>(
            () => new TodoList("Ev", ownerUserId));
    }
}
