using TodoApi.Application.Exceptions;
using TodoApi.Application.Features.Todos.CreateTodo;
using TodoApi.Application.Features.Todos.DeleteTodo;
using TodoApi.Application.Features.Todos.GetTodoById;
using TodoApi.Application.Features.Todos.GetTodosInList;
using TodoApi.Application.Features.Todos.SetTodoCompletionStatus;
using TodoApi.Application.Features.Todos.UpdateTodo;
using TodoApi.Domain.Enums;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class TodoUseCaseTests
{
    [Fact]
    public async Task CreateTodo_AddsTheTaskToItsListAndSavesIt()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        CreateTodoUseCase useCase = CreateTodoUseCaseFor(repository);

        var result = await useCase.ExecuteAsync(
            new CreateTodoCommand
            {
                TodoListId = savedList.Id,
                Title = "  Raporu tamamla  ",
                Description = "  Sonuçları ekle  ",
                Priority = TodoPriority.High
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(savedList.Id, result.TodoListId);
        Assert.Equal("Raporu tamamla", result.Title);
        Assert.Equal("Sonuçları ekle", result.Description);
        Assert.Equal(TodoPriority.High, result.Priority);
        Assert.Single(savedList.Todos);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task CreateTodo_ReturnsNullWhenTheListDoesNotExist()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        CreateTodoUseCase useCase = CreateTodoUseCaseFor(repository);

        var result = await useCase.ExecuteAsync(
            new CreateTodoCommand { TodoListId = 404, Title = "Raporu tamamla" },
            TestContext.Current.CancellationToken);

        Assert.Null(result);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task GetTodosInList_ReturnsTheTasksInThatList()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        repository.AddExistingTodoItem(savedList, "Raporu tamamla");
        repository.AddExistingTodoItem(savedList, "Toplantıya hazırlan");
        GetTodosInListUseCase useCase = new GetTodosInListUseCase(repository, FakeCurrentUser.ListOwner());

        var results = await useCase.ExecuteAsync(
            savedList.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(results);
        Assert.Collection(
            results,
            first => Assert.Equal("Raporu tamamla", first.Title),
            second => Assert.Equal("Toplantıya hazırlan", second.Title));
    }

    [Fact]
    public async Task GetTodoById_ReturnsTheMatchingTask()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Raporu tamamla");
        GetTodoByIdUseCase useCase = new GetTodoByIdUseCase(repository, FakeCurrentUser.ListOwner());

        var result = await useCase.ExecuteAsync(
            savedList.Id,
            savedTodo.Id,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(savedTodo.Id, result.Id);
        Assert.Equal("Raporu tamamla", result.Title);
    }

    [Fact]
    public async Task UpdateTodo_ChangesTheTaskFieldsAndSavesThem()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Eski başlık");
        DateTime dueAtUtc = new DateTime(2026, 9, 26, 15, 0, 0, DateTimeKind.Utc);
        UpdateTodoUseCase useCase = new UpdateTodoUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new UpdateTodoCommandValidator(FakeTimeProvider.AtTestStart()),
            FakeTimeProvider.AtTestStart());

        var result = await useCase.ExecuteAsync(
            new UpdateTodoCommand
            {
                TodoListId = savedList.Id,
                TodoId = savedTodo.Id,
                Title = "  Yeni başlık  ",
                Description = "  Yeni açıklama  ",
                DueAtUtc = dueAtUtc,
                Priority = TodoPriority.High
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Yeni başlık", result.Title);
        Assert.Equal("Yeni açıklama", result.Description);
        Assert.Equal(dueAtUtc, result.DueAtUtc);
        Assert.Equal(TodoPriority.High, result.Priority);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task SetTodoCompletionStatus_UpdatesTheCompletionValue()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Raporu tamamla");
        SetTodoCompletionStatusUseCase useCase =
            new SetTodoCompletionStatusUseCase(repository, FakeCurrentUser.ListOwner());

        var result = await useCase.ExecuteAsync(
            savedList.Id,
            savedTodo.Id,
            isCompleted: true,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.IsCompleted);
        Assert.True(savedTodo.IsCompleted);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task DeleteTodo_RemovesTheTaskAndSavesTheChange()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Raporu tamamla");
        DeleteTodoUseCase useCase = new DeleteTodoUseCase(repository, FakeCurrentUser.ListOwner());

        bool wasDeleted = await useCase.ExecuteAsync(
            savedList.Id,
            savedTodo.Id,
            TestContext.Current.CancellationToken);

        Assert.True(wasDeleted);
        Assert.Empty(savedList.Todos);
        Assert.Equal(1, repository.SaveChangesCount);
    }

    [Fact]
    public async Task DeleteTodo_ReturnsFalseWhenTheTaskDoesNotExist()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        DeleteTodoUseCase useCase = new DeleteTodoUseCase(repository, FakeCurrentUser.ListOwner());

        bool wasDeleted = await useCase.ExecuteAsync(
            savedList.Id,
            404,
            TestContext.Current.CancellationToken);

        Assert.False(wasDeleted);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task UpdateTodo_KeepsAnOverdueDueDateWhenOnlyOtherFieldsChange()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        var overdueTodo = repository.AddExistingTodoItem(savedList, "Gecikmiş iş");
        DateTime pastDueAtUtc = new DateTime(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);
        overdueTodo.SetDueAtUtc(pastDueAtUtc);

        var result = await CreateUpdateTodoUseCaseFor(repository).ExecuteAsync(
            new UpdateTodoCommand
            {
                TodoListId = savedList.Id,
                TodoId = overdueTodo.Id,
                Title = "Gecikmiş iş (yazım düzeltildi)",
                DueAtUtc = pastDueAtUtc
            },
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Gecikmiş iş (yazım düzeltildi)", result.Title);
        Assert.Equal(pastDueAtUtc, result.DueAtUtc);
    }

    [Fact]
    public async Task UpdateTodo_RejectsMovingTheDueDateIntoThePastAndChangesNothing()
    {
        FakeTodoListRepository repository = new FakeTodoListRepository();
        var savedList = repository.AddExistingTodoList("İş");
        var savedTodo = repository.AddExistingTodoItem(savedList, "Raporu yaz");

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => CreateUpdateTodoUseCaseFor(repository).ExecuteAsync(
                new UpdateTodoCommand
                {
                    TodoListId = savedList.Id,
                    TodoId = savedTodo.Id,
                    Title = "Yeni başlık",
                    DueAtUtc = new DateTime(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc)
                },
                TestContext.Current.CancellationToken));

        Assert.Equal("'Bitiş tarihi' geçmişte olamaz.", exception.Errors[nameof(UpdateTodoCommand.DueAtUtc)][0]);
        Assert.Equal("Raporu yaz", savedTodo.Title);
        Assert.Null(savedTodo.DueAtUtc);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    private static UpdateTodoUseCase CreateUpdateTodoUseCaseFor(FakeTodoListRepository repository)
    {
        return new UpdateTodoUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new UpdateTodoCommandValidator(FakeTimeProvider.AtTestStart()),
            FakeTimeProvider.AtTestStart());
    }

    private static CreateTodoUseCase CreateTodoUseCaseFor(FakeTodoListRepository repository)
    {
        return new CreateTodoUseCase(
            repository,
            FakeCurrentUser.ListOwner(),
            new CreateTodoCommandValidator(FakeTimeProvider.AtTestStart()));
    }
}
