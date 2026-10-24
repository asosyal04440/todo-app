using TodoApi.Application.Common.Results;
using TodoApi.Application.Features.TodoLists.CreateTodoList;
using TodoApi.Application.Features.TodoLists.DeleteTodoList;
using TodoApi.Application.Features.TodoLists.GetTodoListById;
using TodoApi.Application.Features.TodoLists.GetTodoLists;
using TodoApi.Application.Features.TodoLists.RenameTodoList;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.DeleteTodo;
using TodoApi.Application.Features.Todos.GetTodosInList;
using TodoApi.Application.Features.Todos.SetTodoCompletionStatus;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class TodoListAuthorizationTests
{
    [Fact]
    public async Task CreateTodoList_MakesTheCurrentUserTheOwner()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        CreateTodoListUseCase useCase = new CreateTodoListUseCase(repository.UnitOfWork,
            repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForCreateTodoList(new CreateTodoListCommandValidator()));

        await useCase.ExecuteAsync(
            new CreateTodoListCommand { Name = "Ev" },
            TestContext.Current.CancellationToken);

        Assert.Equal(FakeCurrentUser.OtherUserId, Assert.Single(repository.TodoLists).OwnerUserId);
    }

    [Fact]
    public async Task GetTodoLists_ReturnsOnlyTheCurrentUsersLists()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        repository.AddExistingTodoList("Benim listem", FakeCurrentUser.ListOwnerUserId);
        repository.AddExistingTodoList("Başkasının listesi", FakeCurrentUser.OtherUserId);
        GetTodoListsUseCase useCase = new GetTodoListsUseCase(repository,
            FakeCurrentUser.ListOwner(),
            TestUseCasePipelines.ForGetTodoLists());

        var results = TestResults.Success(await useCase.ExecuteAsync(TestContext.Current.CancellationToken));

        Assert.Equal("Benim listem", Assert.Single(results.Items).Name);
    }

    [Fact]
    public async Task GetTodoListById_HidesAnotherUsersList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        GetTodoListByIdUseCase useCase = new GetTodoListByIdUseCase(repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForGetTodoListById());

        var result = await useCase.ExecuteAsync(savedList.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task GetTodoListById_LetsAnAdminSeeAnyList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        GetTodoListByIdUseCase useCase = new GetTodoListByIdUseCase(repository,
            FakeCurrentUser.Admin(),
            TestUseCasePipelines.ForGetTodoListById());

        var result = TestResults.Success(await useCase.ExecuteAsync(savedList.Id, TestContext.Current.CancellationToken));

        Assert.NotNull(result);
        Assert.Equal("Ev", result.Name);
    }

    [Fact]
    public async Task RenameTodoList_DoesNotChangeAnotherUsersList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        RenameTodoListUseCase useCase = new RenameTodoListUseCase(repository.UnitOfWork,
            repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForRenameTodoList(new RenameTodoListCommandValidator()));

        var result = await useCase.ExecuteAsync(
            new RenameTodoListCommand { TodoListId = savedList.Id, Name = "Ele geçirildi" },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, result.Error!.Code);
        Assert.Equal("Ev", savedList.Name);
        Assert.Equal(0, repository.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task DeleteTodoList_DoesNotDeleteAnotherUsersList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        DeleteTodoListUseCase useCase = new DeleteTodoListUseCase(repository.UnitOfWork,
            repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForDeleteTodoList());

        var wasDeleted = await useCase.ExecuteAsync(savedList.Id, TestContext.Current.CancellationToken);

        Assert.True(wasDeleted.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, wasDeleted.Error!.Code);
        Assert.Single(repository.TodoLists);
        Assert.Equal(0, repository.UnitOfWork.SaveChangesCount);
    }

    [Fact]
    public async Task CreateTodo_DoesNotAddATaskToAnotherUsersList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        CreateTodoUseCase useCase = new CreateTodoUseCase(repository.UnitOfWork,
            repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForCreateTodo(new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart())));

        var result = await useCase.ExecuteAsync(
            new CreateTodoCommand { TodoListId = savedList.Id, Title = "İzinsiz görev" },
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, result.Error!.Code);
        Assert.Empty(savedList.Todos);
    }

    [Fact]
    public async Task GetTodosInList_HidesTheTasksOfAnotherUsersList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        repository.AddExistingTodoItem(savedList, "Gizli görev");
        GetTodosInListUseCase useCase = new GetTodosInListUseCase(repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForGetTodosInList());

        var result = await useCase.ExecuteAsync(savedList.Id, TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task SetTodoCompletionStatus_DoesNotChangeAnotherUsersTask()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Görev");
        SetTodoCompletionStatusUseCase useCase = new SetTodoCompletionStatusUseCase(repository.UnitOfWork,
            repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForSetTodoCompletionStatus());

        var result = await useCase.ExecuteAsync(
            savedList.Id,
            savedTodo.Id,
            true,
            TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, result.Error!.Code);
        Assert.False(savedTodo.IsCompleted);
    }

    [Fact]
    public async Task DeleteTodo_DoesNotDeleteAnotherUsersTask()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("Ev");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Görev");
        DeleteTodoUseCase useCase = new DeleteTodoUseCase(repository.UnitOfWork,
            repository,
            FakeCurrentUser.OtherUser(),
            TestUseCasePipelines.ForDeleteTodo());

        var wasDeleted = await useCase.ExecuteAsync(
            savedList.Id,
            savedTodo.Id,
            TestContext.Current.CancellationToken);

        Assert.True(wasDeleted.IsFailure);
        Assert.Equal(ErrorCode.AccessDenied, wasDeleted.Error!.Code);
        Assert.Single(savedList.Todos);
    }
}
