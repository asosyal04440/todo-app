using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Common.Execution;
using TodoApi.Application.Common.Results;
using TodoApi.Application.Contracts;

namespace TodoApi.Application.Features.Users.GetUsers;

public class GetUsersUseCase(IUserReadQueries queries, UseCasePipeline<PageRequest, PagedResult<UserDto>> pipeline) : UseCase<PageRequest, PagedResult<UserDto>>(pipeline)
{
    public Task<Result<PagedResult<UserDto>>> ExecuteAsync(CancellationToken cancellationToken = default) => base.ExecuteAsync(new PageRequest(), cancellationToken);
    protected override async Task<Result<PagedResult<UserDto>>> HandleAsync(PageRequest page, CancellationToken cancellationToken) =>
        Result<PagedResult<UserDto>>.Success(await queries.GetUsersAsync(page, cancellationToken));
}