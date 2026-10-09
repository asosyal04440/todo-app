using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;
using TodoApi.Domain.Entities;
using todo.Application.Tests.TestDoubles;

namespace todo.Application.Tests;

public class TodoReminderUseCaseTests
{
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTodoReminderRepository _repository = new FakeTodoReminderRepository();
    private readonly FakeEmailSender _emailSender = new FakeEmailSender();
    private readonly FakeTimeProvider _timeProvider = new FakeTimeProvider(Now);

    [Fact]
    public async Task Execute_SendsAReminderToTheOwnerAndMarksTheTodo()
    {
        TodoItem todo = AddCandidate("Raporu teslim et", Now.UtcDateTime.AddHours(3), "ali@example.com");

        ReminderRunResult result = await CreateUseCase().ExecuteAsync(TestContext.Current.CancellationToken);

        var message = Assert.Single(_emailSender.SentMessages);
        Assert.Equal("ali@example.com", message.ToAddress);
        Assert.Contains("Raporu teslim et", message.Subject);
        Assert.Contains("06.10.2026 18:00", message.PlainTextBody);
        Assert.Equal(Now.UtcDateTime, todo.ReminderSentAtUtc);
        Assert.Equal(1, result.SentCount);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task Execute_DoesNotSendTheSameReminderTwice()
    {
        AddCandidate("Raporu teslim et", Now.UtcDateTime.AddHours(3), "ali@example.com");
        SendDueTodoRemindersUseCase useCase = CreateUseCase();

        await useCase.ExecuteAsync(TestContext.Current.CancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Single(_emailSender.SentMessages);
    }

    [Fact]
    public async Task Execute_SkipsTodosThatAreNotDueSoon()
    {
        AddCandidate("Gelecek hafta", Now.UtcDateTime.AddDays(7), "ali@example.com");
        AddCandidate("Süresi geçmiş", Now.UtcDateTime.AddHours(-1), "ali@example.com");

        ReminderRunResult result = await CreateUseCase().ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Empty(_emailSender.SentMessages);
        Assert.Equal(0, result.CandidateCount);
    }

    [Fact]
    public async Task Execute_RetriesAFailedReminderOnTheNextRunWithoutBlockingOthers()
    {
        TodoItem failingTodo = AddCandidate("Mail gitmeyecek", Now.UtcDateTime.AddHours(2), "bozuk@example.com");
        TodoItem workingTodo = AddCandidate("Mail gidecek", Now.UtcDateTime.AddHours(3), "ali@example.com");
        _emailSender.FailFor("bozuk@example.com");
        SendDueTodoRemindersUseCase useCase = CreateUseCase();

        ReminderRunResult firstRun = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, firstRun.SentCount);
        Assert.Equal(failingTodo.Id, Assert.Single(firstRun.Failures).TodoId);
        Assert.Null(failingTodo.ReminderSentAtUtc);
        Assert.NotNull(workingTodo.ReminderSentAtUtc);

        _emailSender.StopFailing();
        _timeProvider.Advance(TimeSpan.FromMinutes(15));
        ReminderRunResult secondRun = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, secondRun.SentCount);
        Assert.NotNull(failingTodo.ReminderSentAtUtc);
        Assert.Equal(2, _emailSender.SentMessages.Count);
    }

    [Fact]
    public async Task Execute_EncodesUserTextInTheHtmlBody()
    {
        AddCandidate(
            "<a href=\"https://kotu.example.com\">Tıkla</a>",
            Now.UtcDateTime.AddHours(3),
            "ali@example.com");

        await CreateUseCase().ExecuteAsync(TestContext.Current.CancellationToken);

        var message = Assert.Single(_emailSender.SentMessages);
        Assert.DoesNotContain("<a href", message.HtmlBody);
        Assert.Contains("&lt;a href=", message.HtmlBody);
    }

    [Fact]
    public async Task Execute_SendsAtMostTheConfiguredBatchSize()
    {
        for (int index = 0; index < 5; index++)
        {
            AddCandidate($"Görev {index}", Now.UtcDateTime.AddHours(1 + index), "ali@example.com");
        }

        ReminderRunResult result = await CreateUseCase(batchSize: 2)
            .ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, result.SentCount);
        Assert.Equal(2, _emailSender.SentMessages.Count);
    }

    [Fact]
    public async Task Execute_DoesNotLetRejectedRemindersBlockTheOnesBehindThem()
    {
        AddCandidate("Kapalı adres 1", Now.UtcDateTime.AddHours(1), "kapali@example.com");
        AddCandidate("Kapalı adres 2", Now.UtcDateTime.AddHours(2), "kapali@example.com");
        TodoItem laterTodo = AddCandidate("Sağlam adres", Now.UtcDateTime.AddHours(5), "ali@example.com");
        _emailSender.FailFor("kapali@example.com");
        SendDueTodoRemindersUseCase useCase = CreateUseCase(batchSize: 2);

        ReminderRunResult firstRun = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);
        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        ReminderRunResult secondRun = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(0, firstRun.SentCount);
        Assert.Equal(2, firstRun.Failures.Count);
        Assert.Equal(1, secondRun.SentCount);
        Assert.NotNull(laterTodo.ReminderSentAtUtc);
    }

    [Fact]
    public async Task Execute_GivesUpOnARejectedReminderAfterTheMaximumAttempts()
    {
        TodoItem todo = AddCandidate("Kapalı adres", Now.UtcDateTime.AddHours(3), "kapali@example.com");
        _emailSender.FailFor("kapali@example.com");
        SendDueTodoRemindersUseCase useCase = CreateUseCase();
        List<ReminderRunResult> runs = new List<ReminderRunResult>();

        for (int run = 0; run < 5; run++)
        {
            runs.Add(await useCase.ExecuteAsync(TestContext.Current.CancellationToken));
            _timeProvider.Advance(TimeSpan.FromMinutes(15));
        }

        Assert.Equal(3, _emailSender.AttemptCount);
        Assert.Equal(3, todo.ReminderAttemptCount);
        Assert.True(runs[0].Failures[0].WillBeRetried);
        Assert.False(runs[2].Failures[0].WillBeRetried);
        Assert.Empty(runs[4].Failures);
    }

    [Fact]
    public async Task Execute_StopsTheRunWithoutPenalizingTodosWhenTheMailServerIsDown()
    {
        TodoItem firstTodo = AddCandidate("Görev 1", Now.UtcDateTime.AddHours(1), "ali@example.com");
        TodoItem secondTodo = AddCandidate("Görev 2", Now.UtcDateTime.AddHours(2), "ayse@example.com");
        _emailSender.MakeServiceUnavailable();
        SendDueTodoRemindersUseCase useCase = CreateUseCase();

        ReminderRunResult result = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.True(result.EmailServiceUnavailable);
        Assert.Equal(1, _emailSender.AttemptCount);
        Assert.Equal(0, firstTodo.ReminderAttemptCount);
        Assert.Equal(0, secondTodo.ReminderAttemptCount);

        _emailSender.StopFailing();
        _timeProvider.Advance(TimeSpan.FromMinutes(5));
        ReminderRunResult nextRun = await useCase.ExecuteAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, nextRun.SentCount);
    }

    private TodoItem AddCandidate(string title, DateTime dueAtUtc, string ownerEmail)
    {
        TodoList todoList = new TodoList("İş", 1);
        TodoItem todo = todoList.AddTodo(title, dueAtUtc: dueAtUtc);

        _repository.Candidates.Add(new TodoReminderCandidate(todo, todoList.Name, 1, ownerEmail, "Ali"));

        return todo;
    }

    private SendDueTodoRemindersUseCase CreateUseCase(int batchSize = 50)
    {
        ReminderSettings settings = new ReminderSettings(
            isEnabled: true,
            leadTime: TimeSpan.FromHours(24),
            schedule: "*/5 * * * *",
            batchSize: batchSize,
            displayTimeZone: TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"),
            maximumAttempts: 3,
            retryDelay: TimeSpan.FromMinutes(15));

        return new SendDueTodoRemindersUseCase(_repository, _emailSender, settings, _timeProvider);
    }
}
