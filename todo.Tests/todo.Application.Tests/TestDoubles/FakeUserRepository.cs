using System.Reflection;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace todo.Application.Tests.TestDoubles;

public sealed class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new List<User>();
    private int _nextUserId = 1;

    public int SaveChangesCount { get; private set; }

    public IReadOnlyList<User> Users
    {
        get
        {
            return _users;
        }
    }

    public User AddExistingUser(User user)
    {
        SetId(user, _nextUserId);
        _nextUserId++;
        _users.Add(user);

        return user;
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        User? user = _users.Find(existingUser => existingUser.Id == userId);

        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        User? user = _users.Find(existingUser => existingUser.Email == email);

        return Task.FromResult(user);
    }

    public Task<User?> GetByGoogleSubjectIdAsync(
        string googleSubjectId,
        CancellationToken cancellationToken)
    {
        User? user = _users.Find(existingUser => existingUser.GoogleSubjectId == googleSubjectId);

        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<User> results = _users.ToList();

        return Task.FromResult(results);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users.Add(user);

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        foreach (User user in _users)
        {
            if (user.Id == 0)
            {
                SetId(user, _nextUserId);
                _nextUserId++;
            }
        }

        SaveChangesCount++;

        return Task.CompletedTask;
    }

    private static void SetId(User user, int value)
    {
        PropertyInfo property = typeof(User).GetProperty(nameof(User.Id))
            ?? throw new InvalidOperationException("The Id property was not found.");

        MethodInfo setter = property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException("The Id property does not have a setter.");

        setter.Invoke(user, new object[] { value });
    }
}
