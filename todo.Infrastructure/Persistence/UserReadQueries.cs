using Microsoft.EntityFrameworkCore;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;

namespace TodoApi.Infrastructure.Persistence;

public sealed class UserReadQueries(TodoDbContext db) : IUserReadQueries
{
    public Task<PagedResult<UserDto>> GetUsersAsync(PageRequest page, CancellationToken cancellationToken) =>
        ReadQueryPaging.ReadPageAsync(db.Users.AsNoTracking().OrderBy(user => user.Id)
            .Select(user => new UserDto(user.Id, user.Email, user.DisplayName, user.Role,
                user.PasswordHash != null, user.GoogleSubjectId != null, user.CreatedAtUtc)), page, cancellationToken);
}
