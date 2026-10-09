namespace TodoApi.Application.Abstractions.Authentication;

public interface IPasswordHasher
{
    string HashPassword(string password);

    PasswordVerificationOutcome VerifyPassword(string passwordHash, string providedPassword);
}

public enum PasswordVerificationOutcome
{
    Failed = 0,
    Succeeded = 1,
    SucceededRehashNeeded = 2
}
