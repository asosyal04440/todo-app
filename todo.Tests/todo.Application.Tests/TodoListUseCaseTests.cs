using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.DeleteTodoList;
using TodoApi.Application.Features.TodoLists.GetTodoListById;
using TodoApi.Application.Features.TodoLists.GetTodoLists;
using TodoApi.Application.Features.TodoLists.RenameTodoList;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class TodoListUseCaseTests
{
    [Fact]
    public async Task CreateTodoList_SavesTheListAndReturnsItsData()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        CreateTodoListUseCase useCase = new CreateTodoListUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new CreateTodoListCommandValidator());

        var result = await useCase.ExecuteAsync(
            new CreateTodoListCommand { Name = "  İş  " },
            TestContext.Current.CancellationToken);

        Assert.Equal(1, result.Id);
        Assert.Equal("İş", result.Name);
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.Single(repository.TodoLists);
    }

    [Fact]
    public async Task GetTodoLists_ReturnsEverySavedList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        repository.AddExistingTodoList("İş");
        repository.AddExistingTodoList("Ev");
        GetTodoListsUseCase useCase = new GetTodoListsUseCase(repository, FakeCurrentUser.ListOwner());

        var results = await useCase.ExecuteAsync(
            TestContext.Current.CancellationToken);

        Assert.Collection(
            results,
            first => Assert.Equal("İş", first.Name),
            second => Assert.Equal("Ev", second.Name));
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task GetTodoListById_ReturnsTheMatchingList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        GetTodoListByIdUseCase useCase = new GetTodoListByIdUseCase(repository, FakeCurrentUser.ListOwner());

        var result = await useCase.ExecuteAsync(
            savedList.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(savedList.Id, result.Id);
        Assert.Equal("Ev", result.Name);
    }

    [Fact]
    public async Task GetTodoListById_ReturnsNullWhenTheListDoesNotExist()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        GetTodoListByIdUseCase useCase = new GetTodoListByIdUseCase(repository, FakeCurrentUser.ListOwner());

        var result = await useCase.ExecuteAsync(
            404,
            TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task RenameTodoList_ChangesTheNameAndSavesTheChange()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        RenameTodoListUseCase useCase = new RenameTodoListUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new RenameTodoListCommandValidator());

        var result = await useCase.ExecuteAsync(
            new RenameTodoListCommand { TodoListId = savedList.Id, Name = "  Alışveriş  " },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Alışveriş", result.Name);
        Assert.Equal("Alışveriş", savedList.Name);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task DeleteTodoList_RemovesTheListAndSavesTheChange()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        DeleteTodoListUseCase useCase = new DeleteTodoListUseCase(repository, FakeCurrentUser.ListOwner());

        bool wasDeleted = await useCase.ExecuteAsync(
            savedList.Id,
            TestContext.Current.CancellationToken);

        Assert.True(wasDeleted);
        Assert.Empty(repository.TodoLists);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task DeleteTodoList_ReturnsFalseWhenTheListDoesNotExist()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        DeleteTodoListUseCase useCase = new DeleteTodoListUseCase(repository, FakeCurrentUser.ListOwner());

        bool wasDeleted = await useCase.ExecuteAsync(
            404,
            TestContext.Current.CancellationToken);

        Assert.False(wasDeleted);
        Assert.Equal(0, repository.SaveChangesCount);
    }
}
