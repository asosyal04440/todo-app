using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.API.Authentication;
using TodoApi.API.Models.Requests;
using TodoApi.API.Models.Responses;
using TodoApi.Application.Contracts;
using TodoApi.Application.Features.Users.GetUsers;

namespace TodoApi.API.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public sealed class AdminUsersController(GetUsersUseCase getUsers) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] PageQuery query, CancellationToken cancellationToken) =>
        (await getUsers.ExecuteAsync(query.ToPageRequest(), cancellationToken)).ToActionResult(this);
}