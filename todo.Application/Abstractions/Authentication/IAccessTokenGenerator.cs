using TodoApi.Domain.Entities;

namespace TodoApi.Application.Abstractions.Authentication;

public interface IAccessTokenGenerator
{
    AccessToken GenerateToken(User user);
}
