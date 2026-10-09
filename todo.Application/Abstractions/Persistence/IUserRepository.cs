using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetByGoogleSubjectIdAsync(string googleSubjectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
