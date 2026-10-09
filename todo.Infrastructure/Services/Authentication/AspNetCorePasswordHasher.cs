using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using TodoApi.Application.Abstractions.Authentication;
using TodoApi.Domain.Entities;

namespace TodoApi.Infrastructure.Services.Authentication;

public sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    public const int IterationCount = 210_000;

    private readonly PasswordHasher<User> _passwordHasher = new PasswordHasher<User>(
        Options.Create(new PasswordHasherOptions { IterationCount = IterationCount }));

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("A password cannot be empty.", nameof(password));
        }

        string passwordHash = _passwordHasher.HashPassword(null!, password);

        return passwordHash;
    }

    public PasswordVerificationOutcome VerifyPassword(string passwordHash, string providedPassword)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(providedPassword))
        {
            return PasswordVerificationOutcome.Failed;
        }

        PasswordVerificationResult result;

        try
        {
            result = _passwordHasher.VerifyHashedPassword(null!, passwordHash, providedPassword);
        }
        catch (FormatException)
        {
            return PasswordVerificationOutcome.Failed;
        }

        if (result == PasswordVerificationResult.Success)
        {
            return PasswordVerificationOutcome.Succeeded;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            return PasswordVerificationOutcome.SucceededRehashNeeded;
        }

        return PasswordVerificationOutcome.Failed;
    }
}
