using System;
using System.Collections.Generic;

namespace TodoApi.Application.Features.Reminders.SendDueTodoReminders;

public sealed class ReminderRunResult
{
    private readonly int _candidateCount;
    private readonly int _sentCount;
    private readonly IReadOnlyList<ReminderFailure> _failures;
    private readonly bool _emailServiceUnavailable;

    public ReminderRunResult(
        int candidateCount,
        int sentCount,
        IReadOnlyList<ReminderFailure> failures,
        bool emailServiceUnavailable)
    {
        _candidateCount = candidateCount;
        _sentCount = sentCount;
        _failures = failures ?? throw new ArgumentNullException(nameof(failures));
        _emailServiceUnavailable = emailServiceUnavailable;
    }

    public int CandidateCount
    {
        get
        {
            return _candidateCount;
        }
    }

    public int SentCount
    {
        get
        {
            return _sentCount;
        }
    }

    public IReadOnlyList<ReminderFailure> Failures
    {
        get
        {
            return _failures;
        }
    }

    public bool EmailServiceUnavailable
    {
        get
        {
            return _emailServiceUnavailable;
        }
    }
}

public sealed class ReminderFailure
{
    private readonly int _todoId;
    private readonly int _ownerUserId;
    private readonly Exception _exception;
    private readonly bool _willBeRetried;

    public ReminderFailure(int todoId, int ownerUserId, Exception exception, bool willBeRetried)
    {
        _todoId = todoId;
        _ownerUserId = ownerUserId;
        _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        _willBeRetried = willBeRetried;
    }

    public int TodoId
    {
        get
        {
            return _todoId;
        }
    }

    public int OwnerUserId
    {
        get
        {
            return _ownerUserId;
        }
    }

    public Exception Exception
    {
        get
        {
            return _exception;
        }
    }

    public bool WillBeRetried
    {
        get
        {
            return _willBeRetried;
        }
    }
}
