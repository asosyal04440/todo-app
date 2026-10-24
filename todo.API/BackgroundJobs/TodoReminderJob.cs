using Hangfire;
using TodoApi.Application.Features.Reminders.SendDueTodoReminders;

namespace TodoApi.API.BackgroundJobs;

public sealed class TodoReminderJob
{
    public const string RecurringJobId = "todo-due-date-reminders";

    private readonly SendDueTodoRemindersUseCase _sendDueTodoRemindersUseCase;
    private readonly ILogger<TodoReminderJob> _logger;

    public TodoReminderJob(
        SendDueTodoRemindersUseCase sendDueTodoRemindersUseCase,
        ILogger<TodoReminderJob> logger)
    {
        _sendDueTodoRemindersUseCase = sendDueTodoRemindersUseCase;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    [AutomaticRetry(Attempts = 3)]
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var outcome = await _sendDueTodoRemindersUseCase.ExecuteAsync(cancellationToken);
        if (outcome.IsFailure)
            throw new InvalidOperationException($"The reminder run was rejected: {outcome.Error!.Code}.");
        ReminderRunResult result = outcome.Value;

        _logger.LogInformation(
            "Reminder run finished: {SentCount} sent, {FailedCount} failed, {CandidateCount} candidates.",
            result.SentCount,
            result.Failures.Count,
            result.CandidateCount);

        foreach (ReminderFailure failure in result.Failures)
        {
            if (failure.WillBeRetried)
            {
                _logger.LogWarning(
                    failure.Exception,
                    "The reminder for todo {TodoId} (user {UserId}) could not be sent and will be retried later.",
                    failure.TodoId,
                    failure.OwnerUserId);
            }
            else
            {
                _logger.LogError(
                    failure.Exception,
                    "The reminder for todo {TodoId} (user {UserId}) could not be sent and will not be retried.",
                    failure.TodoId,
                    failure.OwnerUserId);
            }
        }

        if (result.EmailServiceUnavailable)
        {
            throw new InvalidOperationException(
                "The mail server is unavailable, so the reminder run stopped early. " +
                "Remaining reminders will be sent on a later run.");
        }
    }
}
