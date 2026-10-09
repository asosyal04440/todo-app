using System;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Authentication.GetCurrentUser;

public class GetCurrentUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserUseCase(IUserRepository userRepository, ICurrentUser currentUser)
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

    public async Task<UserDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        User? user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        UserDto response = new UserDto(user);

        return response;
    }
}
