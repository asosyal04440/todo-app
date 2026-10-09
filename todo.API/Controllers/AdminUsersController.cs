using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Authentication;
using TodoApi.Application.Contracts;
using TodoApi.Application.Features.Users.GetUsers;

namespace TodoApi.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminUsersController : ControllerBase
{
    private readonly GetUsersUseCase _getUsersUseCase;

    public AdminUsersController(GetUsersUseCase getUsersUseCase)
    {
        _getUsersUseCase = getUsersUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<UserDto> users = await _getUsersUseCase.ExecuteAsync(cancellationToken);

        return Ok(users);
    }
}
