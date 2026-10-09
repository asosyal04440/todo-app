using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Reminders.SendDueTodoReminders;

public class SendDueTodoRemindersUseCase
{
    private readonly ITodoReminderRepository _todoReminderRepository;
    private readonly IEmailSender _emailSender;
    private readonly ReminderSettings _reminderSettings;
    private readonly TimeProvider _timeProvider;

    public SendDueTodoRemindersUseCase(
        ITodoReminderRepository todoReminderRepository,
        IEmailSender emailSender,
        ReminderSettings reminderSettings,
        TimeProvider timeProvider)
    {
        if (todoReminderRepository == null)
        {
            throw new ArgumentNullException(nameof(todoReminderRepository));
        }

        if (emailSender == null)
        {
            throw new ArgumentNullException(nameof(emailSender));
        }

        if (reminderSettings == null)
        {
            throw new ArgumentNullException(nameof(reminderSettings));
        }

        if (timeProvider == null)
        {
            throw new ArgumentNullException(nameof(timeProvider));
        }

        _todoReminderRepository = todoReminderRepository;
        _emailSender = emailSender;
        _reminderSettings = reminderSettings;
        _timeProvider = timeProvider;
    }

    public async Task<ReminderRunResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        DateTime dueBeforeUtc = nowUtc.Add(_reminderSettings.LeadTime);
        DateTime lastFailedAttemptBeforeUtc = nowUtc.Subtract(_reminderSettings.RetryDelay);

        IReadOnlyList<TodoReminderCandidate> candidates =
            await _todoReminderRepository.GetTodosDueForReminderAsync(
                nowUtc,
                dueBeforeUtc,
                lastFailedAttemptBeforeUtc,
                _reminderSettings.MaximumAttempts,
                _reminderSettings.BatchSize,
                cancellationToken);

        int sentCount = 0;
        bool emailServiceUnavailable = false;
        List<ReminderFailure> failures = new List<ReminderFailure>();

        foreach (TodoReminderCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TodoItem todo = candidate.Todo;
            bool needsReminder = todo.NeedsReminder(nowUtc, _reminderSettings.LeadTime);
            bool canRetry = todo.CanRetryReminder(nowUtc, _reminderSettings.MaximumAttempts, _reminderSettings.RetryDelay);

            if (needsReminder == false || canRetry == false)
            {
                continue;
            }

            try
            {
                EmailMessage message = TodoReminderEmailComposer.Compose(
                    candidate,
                    _reminderSettings.DisplayTimeZone);

                await _emailSender.SendAsync(message, cancellationToken);
            }
            catch (EmailServiceUnavailableException exception)
            {
                failures.Add(new ReminderFailure(todo.Id, candidate.OwnerUserId, exception, willBeRetried: true));
                emailServiceUnavailable = true;
                break;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                todo.RecordFailedReminderAttempt(_timeProvider.GetUtcNow().UtcDateTime);
                await _todoReminderRepository.SaveChangesAsync(cancellationToken);

                bool willBeRetried = todo.ReminderAttemptCount < _reminderSettings.MaximumAttempts;
                failures.Add(new ReminderFailure(todo.Id, candidate.OwnerUserId, exception, willBeRetried));
                continue;
            }

            todo.MarkReminderSent(_timeProvider.GetUtcNow().UtcDateTime);
            await _todoReminderRepository.SaveChangesAsync(cancellationToken);
            sentCount++;
        }

        return new ReminderRunResult(candidates.Count, sentCount, failures, emailServiceUnavailable);
    }
}
