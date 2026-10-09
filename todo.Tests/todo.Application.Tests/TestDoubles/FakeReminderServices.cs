using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Abstractions.Persistence;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeEmailSender : IEmailSender
{
    private readonly HashSet<string> _failingRecipients = new HashSet<string>();
    private bool _isServiceUnavailable;

    public List<EmailMessage> SentMessages { get; } = new List<EmailMessage>();

    public bool IsConfigured
    {
        get
        {
            return true;
        }
    }

    public void FailFor(string recipient)
    {
        _failingRecipients.Add(recipient);
    }

    public void MakeServiceUnavailable()
    {
        _isServiceUnavailable = true;
    }

    public void StopFailing()
    {
        _failingRecipients.Clear();
        _isServiceUnavailable = false;
    }

    public int AttemptCount { get; private set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        AttemptCount++;

        if (_isServiceUnavailable)
        {
            throw new EmailServiceUnavailableException("The SMTP server is not reachable.", new IOException());
        }

        if (_failingRecipients.Contains(message.ToAddress))
        {
            throw new EmailRejectedException("The mailbox does not exist.", new InvalidOperationException());
        }

        SentMessages.Add(message);

        return Task.CompletedTask;
    }
}

public sealed class FakeTodoReminderRepository : ITodoReminderRepository
{
    public List<TodoReminderCandidate> Candidates { get; } = new List<TodoReminderCandidate>();

    public int SaveChangesCount { get; private set; }

    public Task<IReadOnlyList<TodoReminderCandidate>> GetTodosDueForReminderAsync(
        DateTime nowUtc,
        DateTime dueBeforeUtc,
        DateTime lastFailedAttemptBeforeUtc,
        int maximumAttempts,
        int maxCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TodoReminderCandidate> results = Candidates
            .Where(candidate =>
                candidate.Todo.IsCompleted == false &&
                candidate.Todo.ReminderSentAtUtc == null &&
                candidate.Todo.DueAtUtc > nowUtc &&
                candidate.Todo.DueAtUtc <= dueBeforeUtc &&
                candidate.Todo.ReminderAttemptCount < maximumAttempts &&
                (candidate.Todo.ReminderLastAttemptAtUtc == null ||
                 candidate.Todo.ReminderLastAttemptAtUtc <= lastFailedAttemptBeforeUtc))
            .OrderBy(candidate => candidate.Todo.DueAtUtc)
            .Take(maxCount)
            .ToList();

        return Task.FromResult(results);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCount++;

        return Task.CompletedTask;
    }
}
