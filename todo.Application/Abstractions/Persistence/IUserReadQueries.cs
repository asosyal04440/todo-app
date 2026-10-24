using TodoApi.Application.Contracts;

namespace TodoApi.Application.Abstractions.Persistence;

public interface IUserReadQueries
{
    Task<PagedResult<UserDto>> GetUsersAsync(PageRequest page, CancellationToken cancellationToken);
}
