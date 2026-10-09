using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TodoApi.Application.Abstractions.Persistence;
using TodoApi.Application.Contracts;
using TodoApi.Domain.Entities;

namespace TodoApi.Application.Features.Users.GetUsers;

public class GetUsersUseCase
{
    private readonly IUserRepository _userRepository;

    public GetUsersUseCase(IUserRepository userRepository)
    {
        if (userRepository == null)
        {
            throw new ArgumentNullException(nameof(userRepository));
        }

        _userRepository = userRepository;
    }

    public async Task<IReadOnlyList<UserDto>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<User> users = await _userRepository.GetAllAsync(cancellationToken);

        List<UserDto> responses = new List<UserDto>();

        foreach (User user in users)
        {
            UserDto response = new UserDto(user);
            responses.Add(response);
        }

        return responses;
    }
}
