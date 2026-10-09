using System;
using System.Security.Cryptography;
using System.Text;
using TodoApi.Application.Abstractions.Authentication;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    private const int TokenByteCount = 32;

    public string GenerateToken()
    {
        byte[] tokenBytes = RandomNumberGenerator.GetBytes(TokenByteCount);

        string token = Convert.ToBase64String(tokenBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return token;
    }

    public string ComputeHash(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("A token cannot be empty.", nameof(token));
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));

        return Convert.ToHexString(hashBytes);
    }
}
