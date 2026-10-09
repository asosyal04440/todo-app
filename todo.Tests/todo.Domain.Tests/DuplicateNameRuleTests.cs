using TodoApi.Domain.Common;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Exceptions;

namespace todo.Domain.Tests;

public class DuplicateNameRuleTests
{
    [Theory]
    [InlineData("Raporu yaz", "Raporu yaz")]
    [InlineData("Raporu yaz", "  raporu YAZ  ")]
    [InlineData("İş planı", "iş planı")]
    public void AddTodo_RejectsATitleThatAlreadyExistsInTheList(string existingTitle, string newTitle)
    {
        TodoList todoList = new TodoList("İş", 1);
        todoList.AddTodo(existingTitle);

        Assert.Throws<DuplicateNameException>(() => todoList.AddTodo(newTitle));
        Assert.Single(todoList.Todos);
    }

    [Fact]
    public void AddTodo_AllowsTheSameTitleInDifferentLists()
    {
        TodoList workList = new TodoList("İş", 1);
        TodoList homeList = new TodoList("Ev", 1);

        workList.AddTodo("Alışveriş");
        homeList.AddTodo("Alışveriş");

        Assert.Single(workList.Todos);
        Assert.Single(homeList.Todos);
    }

    [Fact]
    public void RenameTodo_RejectsTheTitleOfAnotherTodoInTheList()
    {
        TodoList todoList = new TodoList("İş", 1);
        todoList.AddTodo("Raporu yaz");
        TodoItem otherTodo = todoList.AddTodo("Toplantı");

        Assert.Throws<DuplicateNameException>(() => todoList.RenameTodo(otherTodo, "RAPORU YAZ"));
        Assert.Equal("Toplantı", otherTodo.Title);
    }

    [Fact]
    public void RenameTodo_AllowsChangingOnlyTheLetterCaseOfItsOwnTitle()
    {
        TodoList todoList = new TodoList("İş", 1);
        TodoItem todo = todoList.AddTodo("raporu yaz");

        todoList.RenameTodo(todo, "Raporu Yaz");

        Assert.Equal("Raporu Yaz", todo.Title);
    }

    [Fact]
    public void RenameTodo_RejectsATodoFromAnotherList()
    {
        TodoList workList = new TodoList("İş", 1);
        TodoList homeList = new TodoList("Ev", 1);
        TodoItem homeTodo = homeList.AddTodo("Bulaşık");

        Assert.Throws<DomainRuleViolationException>(() => workList.RenameTodo(homeTodo, "Yeni ad"));
    }

    [Theory]
    [InlineData("İstanbul", "istanbul", true)]
    [InlineData("Ilık", "ılık", true)]
    [InlineData("Ev", "Ev işleri", false)]
    public void NameComparison_FollowsTurkishCaseRules(string first, string second, bool expected)
    {
        Assert.Equal(expected, NameComparison.AreSame(first, second));
    }
}
