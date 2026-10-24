using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;
using TodoApi.Domain.Specifications;

namespace TodoApi.Application.Features.Reminders.SendDueTodoReminders;

public class SendDueTodoRemindersUseCase : UseCase<EmptyRequest, ReminderRunResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITodoReminderRepository _todoReminderRepository;
    private readonly IEmailSender _emailSender;
    private readonly ReminderSettings _reminderSettings;
    private readonly TimeProvider _timeProvider;

    public SendDueTodoRemindersUseCase(
        IUnitOfWork unitOfWork,
        ITodoReminderRepository todoReminderRepository,
        IEmailSender emailSender,
        ReminderSettings reminderSettings,
        TimeProvider timeProvider,
        UseCasePipeline<EmptyRequest, ReminderRunResult> pipeline) : base(pipeline)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);

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

        _unitOfWork = unitOfWork;
        _todoReminderRepository = todoReminderRepository;
        _emailSender = emailSender;
        _reminderSettings = reminderSettings;
        _timeProvider = timeProvider;
    }

    public Task<Result<ReminderRunResult>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        base.ExecuteAsync(default, cancellationToken);

    protected override async Task<Result<ReminderRunResult>> HandleAsync(
        EmptyRequest request, CancellationToken cancellationToken)
    {
        DateTime nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var specification = new TodoReminderSpecification(
            nowUtc,
            _reminderSettings.LeadTime,
            _reminderSettings.MaximumAttempts,
            _reminderSettings.RetryDelay);

        IReadOnlyList<TodoReminderCandidate> candidates =
            await _todoReminderRepository.GetTodosDueForReminderAsync(
                specification,
                _reminderSettings.BatchSize,
                cancellationToken);

        int sentCount = 0;
        bool emailServiceUnavailable = false;
        List<ReminderFailure> failures = new List<ReminderFailure>();

        foreach (TodoReminderCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TodoItem todo = candidate.Todo;
            if (specification.IsSatisfiedBy(todo) == false)
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                bool willBeRetried = todo.ReminderAttemptCount < _reminderSettings.MaximumAttempts;
                failures.Add(new ReminderFailure(todo.Id, candidate.OwnerUserId, exception, willBeRetried));
                continue;
            }

            todo.MarkReminderSent(_timeProvider.GetUtcNow().UtcDateTime);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            sentCount++;
        }

        return Result<ReminderRunResult>.Success(new ReminderRunResult(candidates.Count, sentCount, failures, emailServiceUnavailable));
    }
}
