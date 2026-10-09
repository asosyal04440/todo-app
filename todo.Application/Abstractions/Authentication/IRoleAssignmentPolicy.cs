using TodoApi.Domain.Enums;

namespace TodoApi.Application.Abstractions.Authentication;

public interface IRoleAssignmentPolicy
{
    UserRole GetRoleForEmail(string email);
}
