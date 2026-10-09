namespace TodoApi.Application.Abstractions.Authentication;

public interface ISecureTokenGenerator
{
    string GenerateToken();

    string ComputeHash(string token);
}
