using TodoApi.Application.Abstractions.Persistence;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Action[] _applyChanges;

    public FakeUnitOfWork(params Action[] applyChanges)
    {
        _applyChanges = applyChanges;
    }

    public int SaveChangesCount { get; private set; }

    public Exception? SaveException { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SaveChangesCount++;

        if (SaveException != null)
        {
            throw SaveException;
        }

        foreach (Action applyChanges in _applyChanges)
        {
            applyChanges();
        }

        return Task.CompletedTask;
    }
}
