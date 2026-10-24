using TodoApi.Application.Common.Results;
using TodoApi.Application.Common.Execution;
using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.GetCurrentUser;

public class GetCurrentUserUseCase : UseCase<EmptyRequest, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserUseCase(IUserRepository userRepository, ICurrentUser currentUser,
        UseCasePipeline<EmptyRequest, UserDto> pipeline) : base(pipeline)
    {
        if (userRepository == null)
        {
            throw new ArgumentNullException(nameof(userRepository));
        }

        if (currentUser == null)
        {
            throw new ArgumentNullException(nameof(currentUser));
        }

        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public Task<Result<UserDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        base.ExecuteAsync(default, cancellationToken);

    protected override async Task<Result<UserDto>> HandleAsync(
        EmptyRequest request, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (user == null)
        {
            return Result<UserDto>.Failure(AppErrors.CurrentUserMissing);
        }

        UserDto response = new UserDto(user);

        return Result<UserDto>.Success(response);
    }
}
