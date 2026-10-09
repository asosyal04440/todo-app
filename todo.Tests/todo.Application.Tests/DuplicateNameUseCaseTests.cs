using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.RenameTodoList;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.UpdateTodo;
using TodoApi.Domain.Exceptions;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class DuplicateNameUseCaseTests
{
    [Fact]
    public async Task CreateTodoList_RejectsAListNameTheUserAlreadyHas()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        repository.AddExistingTodoList("Ev işleri");
        CreateTodoListUseCase useCase = CreateListUseCase(repository, FakeCurrentUser.ListOwner());

        await Assert.ThrowsAsync<DuplicateNameException>(
            () => useCase.ExecuteAsync(
                new CreateTodoListCommand { Name = "  EV İŞLERİ " },
                TestContext.Current.CancellationToken));

        Assert.Single(repository.TodoLists);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task CreateTodoList_AllowsTheSameNameForAnotherUser()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        repository.AddExistingTodoList("Ev işleri", FakeCurrentUser.ListOwnerUserId);
        CreateTodoListUseCase useCase = CreateListUseCase(repository, FakeCurrentUser.OtherUser());

        await useCase.ExecuteAsync(
            new CreateTodoListCommand { Name = "Ev işleri" },
            TestContext.Current.CancellationToken);

        Assert.Equal(2, repository.TodoLists.Count);
    }

    [Fact]
    public async Task CreateTodoList_StopsAtValidationBeforeTouchingTheDatabase()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        CreateTodoListUseCase useCase = CreateListUseCase(repository, FakeCurrentUser.ListOwner());

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => useCase.ExecuteAsync(
                new CreateTodoListCommand { Name = "   " },
                TestContext.Current.CancellationToken));

        Assert.Contains(nameof(CreateTodoListCommand.Name), exception.Errors.Keys);
        Assert.Empty(repository.TodoLists);
    }

    [Fact]
    public async Task RenameTodoList_RejectsTheNameOfAnotherListOfTheSameUser()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        repository.AddExistingTodoList("İş");
        var homeList = repository.AddExistingTodoList("Ev");
        RenameTodoListUseCase useCase = new RenameTodoListUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new RenameTodoListCommandValidator());

        await Assert.ThrowsAsync<DuplicateNameException>(
            () => useCase.ExecuteAsync(
                new RenameTodoListCommand { TodoListId = homeList.Id, Name = "iş" },
                TestContext.Current.CancellationToken));

        Assert.Equal("Ev", homeList.Name);
    }

    [Fact]
    public async Task RenameTodoList_AllowsChangingOnlyTheLetterCaseOfItsOwnName()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var homeList = repository.AddExistingTodoList("ev");
        RenameTodoListUseCase useCase = new RenameTodoListUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new RenameTodoListCommandValidator());

        var result = await useCase.ExecuteAsync(
            new RenameTodoListCommand { TodoListId = homeList.Id, Name = "Ev" },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Ev", homeList.Name);
    }

    [Fact]
    public async Task CreateTodo_RejectsATitleThatAlreadyExistsInTheList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        repository.AddExistingTodoItem(savedList, "Raporu yaz");
        CreateTodoUseCase useCase = new CreateTodoUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart()));

        await Assert.ThrowsAsync<DuplicateNameException>(
            () => useCase.ExecuteAsync(
                new CreateTodoCommand { TodoListId = savedList.Id, Title = "raporu YAZ" },
                TestContext.Current.CancellationToken));

        Assert.Single(savedList.Todos);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task UpdateTodo_RejectsRenamingToAnotherTodosTitle()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        repository.AddExistingTodoItem(savedList, "Raporu yaz");
        var meeting = repository.AddExistingTodoItem(savedList, "Toplantı");
        UpdateTodoUseCase useCase = new UpdateTodoUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new UpdateTodoCommandValidator(FakeTimeProvider.AtTestStart()),
            FakeTimeProvider.AtTestStart());

        await Assert.ThrowsAsync<DuplicateNameException>(
            () => useCase.ExecuteAsync(
                new UpdateTodoCommand { TodoListId = savedList.Id, TodoId = meeting.Id, Title = "Raporu yaz" },
                TestContext.Current.CancellationToken));

        Assert.Equal("Toplantı", meeting.Title);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    private static CreateTodoListUseCase CreateListUseCase(
        FakeTodoListRepository repository,
        FakeCurrentUser currentUser)
    {
        return new CreateTodoListUseCase(repository, currentUser, new CreateTodoListCommandValidator());
    }
}
