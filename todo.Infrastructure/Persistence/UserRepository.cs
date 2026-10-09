using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Persistence;

public sealed class UserRepository : IUserRepository
{
    private readonly TodoDbContext _todoDbContext;

    public UserRepository(TodoDbContext todoDbContext)
    {
        if (todoDbContext == null)
        {
            throw new ArgumentNullException(nameof(todoDbContext));
        }

        _todoDbContext = todoDbContext;
    }

    public async Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        User? user = await _todoDbContext.Users.FirstOrDefaultAsync(
            existingUser => existingUser.Id == userId,
            cancellationToken);

        return user;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        User? user = await _todoDbContext.Users.FirstOrDefaultAsync(
            existingUser => existingUser.Email == email,
            cancellationToken);

        return user;
    }

    public async Task<User?> GetByGoogleSubjectIdAsync(
        string googleSubjectId,
        CancellationToken cancellationToken)
    {
        User? user = await _todoDbContext.Users.FirstOrDefaultAsync(
            existingUser => existingUser.GoogleSubjectId == googleSubjectId,
            cancellationToken);

        return user;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        IQueryable<User> userQuery = _todoDbContext.Users;

        userQuery = userQuery.AsNoTracking();
        userQuery = userQuery.OrderBy(user => user.Id);

        List<User> users = await userQuery.ToListAsync(cancellationToken);

        IReadOnlyList<User> result = users;

        return result;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        await _todoDbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _todoDbContext.SaveChangesAsync(cancellationToken);
    }
}
