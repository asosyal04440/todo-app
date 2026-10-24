using TodoApi.Application.Abstractions.Email;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Specifications;

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

    public FakeUnitOfWork UnitOfWork { get; } = new FakeUnitOfWork();

    public Task<IReadOnlyList<TodoReminderCandidate>> GetTodosDueForReminderAsync(
        TodoReminderSpecification specification,
        int maxCount,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TodoReminderCandidate> results = Candidates
            .Where(candidate => specification.IsSatisfiedBy(candidate.Todo))
            .OrderBy(candidate => candidate.Todo.DueAtUtc)
            .Take(maxCount)
            .ToList();

        return Task.FromResult(results);
    }
}
